using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reactive.Subjects;
using Asv.Tools;

namespace Asv.Gnss.V2
{
    public abstract class GnssMessageParserBase : DisposableOnce, IGnssMessageParser
    {
        #region Metrics

        protected static readonly Meter Meter = new("asv-gnss-parser", "1.0.0");
        protected static readonly Counter<int> MeterInputPackets = Meter.CreateCounter<int>("input", "pkt", "Input packets counter");
        protected static readonly Counter<int> MeterUnknownPackets = Meter.CreateCounter<int>("unknown", "pkt", "Messages with unknown id");
        protected static readonly Counter<int> MeterDeserializeError = Meter.CreateCounter<int>("parse-error", "pkt", "Messages with deserialization error");
        protected static readonly Counter<int> MeterPublicationError = Meter.CreateCounter<int>("publication-error", "pkt", "Count of publication message exceptions");

        #endregion


        protected readonly Subject<GnssParserException> OnErrorSubject = new();
        protected readonly Subject<IGnssMessageBase> OnMessageSubject = new();
        protected readonly KeyValuePair<string, object> ProtocolMetricTag;
        protected readonly KeyValuePair<string, object> MetricTag;

        public abstract string ProtocolId { get; }

        public GnssMessageParserBase(KeyValuePair<string, object> metricTag)
        {
            MetricTag = metricTag;
            // ReSharper disable once VirtualMemberCallInConstructor
            Debug.Assert(ProtocolId != null, nameof(ProtocolId) + " != null");
            ProtocolMetricTag = new KeyValuePair<string, object>("Protocol", ProtocolId);
        }

        public abstract bool Read(byte data);
        public abstract void Reset();

        public IObservable<GnssParserException> OnError => OnErrorSubject;
        public IObservable<IGnssMessageBase> OnMessage => OnMessageSubject;

        protected override void InternalDisposeOnce()
        {
            OnErrorSubject.OnCompleted();
            OnErrorSubject.Dispose();
            OnMessageSubject.OnCompleted();
            OnMessageSubject.Dispose();
        }
    }


    public abstract class GnssMessageParserBase<TMessage, TMsgId> : GnssMessageParserBase
        where TMessage : GnssMessageBase<TMsgId>
    {
        private readonly Dictionary<TMsgId, Func<TMessage>> _factory = new();

        protected GnssMessageParserBase(KeyValuePair<string,object> metricTag):base(metricTag)
        {
            
            
        }

        public void Register(Func<TMessage> factory)
        {
            var pkt = factory();
            _factory.Add(pkt.MessageId, factory);
        }

        protected void InternalOnError(GnssParserException ex)
        {
            OnErrorSubject.OnNext(ex);
        }

        protected void InternalOnMessage(TMessage message)
        {
            OnMessageSubject.OnNext(message);
        }

        protected void ParsePacket(TMsgId id, ref ReadOnlySpan<byte> data)
        {
            MeterInputPackets.Add(1, MetricTag);
            MeterInputPackets.Add(1, MetricTag, ProtocolMetricTag);
            
            if (_factory.TryGetValue(id, out var factory) == false)
            {
                MeterUnknownPackets.Add(1, MetricTag);
                MeterUnknownPackets.Add(1, MetricTag, ProtocolMetricTag);
                MeterUnknownPackets.Add(1, MetricTag, ProtocolMetricTag,new KeyValuePair<string, object>("message-id",id.ToString()));
                InternalOnError(new GnssParserException(ProtocolId, $"Unknown {ProtocolId} packet message number [MSG={id}]"));
                return;
            }

            var message = factory();

            try
            {
                message.Deserialize(ref data);
                MeterInputPackets.Add(1, ProtocolMetricTag, message.MetricTag);
            }
            catch (Exception e)
            {
                MeterDeserializeError.Add(1, MetricTag);
                MeterDeserializeError.Add(1, MetricTag, ProtocolMetricTag);
                MeterDeserializeError.Add(1, MetricTag, ProtocolMetricTag, message.MetricTag);
                InternalOnError(new GnssParserException(ProtocolId, $"Parse {ProtocolId} packet error [MSG={id}]", e));
                return;
            }

            try
            {
                InternalOnMessage(message);
            }
            catch (Exception e)
            {
                MeterPublicationError.Add(1, MetricTag);
                MeterPublicationError.Add(1, ProtocolMetricTag);
                MeterPublicationError.Add(1, ProtocolMetricTag, message.MetricTag);
                InternalOnError(new GnssParserException(ProtocolId, $"Parse {ProtocolId} packet error [MSG={id}]", e));
            }
        }

        protected override void InternalDisposeOnce()
        {
            base.InternalDisposeOnce();
            _factory.Clear();
        }
    }
}