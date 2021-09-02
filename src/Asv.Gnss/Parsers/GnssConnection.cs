﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using NLog;

namespace Asv.Gnss
{
    public class GnssConnection : IGnssConnection
    {
        private readonly IGnssParser[] _parsers;
        private readonly CancellationTokenSource _disposeCancel = new CancellationTokenSource();
        private readonly object _sync = new object();
        private readonly Subject<GnssParserException> _onErrorSubject = new Subject<GnssParserException>();
        private readonly Subject<GnssMessageBase> _onMessageSubject = new Subject<GnssMessageBase>();
        private readonly bool _disposeDataStream;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IDiagnosticSource _diag;
        private readonly SpeedIndicator _rxInd;
        private static IDisposable _portSubscribe;

        public GnssConnection(string connectionString, IDiagnostic diag, params IGnssParser[] parsers) : this(ConnectionStringConvert(connectionString), parsers)
        {
            _disposeDataStream = true;
            _diag = diag[nameof(GnssConnection)];
            _logger.Info($"GNSS connection string: {connectionString}");

            // diagnostic
            _diag.Str["conn"] = connectionString;
            _diag.Speed["rx", "# ##0 b/s"].Increment(0);
            foreach (var parser in parsers)
            {
                _diag.Speed[parser.ProtocolId, "# ##0 pkt/s"].Increment(0);
            }
        }

      

        private static IDataStream ConnectionStringConvert(string connString)
        {
            var p = PortFactory.Create(connString);
            p.Enable();
            return p;
        }

        public GnssConnection(IDataStream stream, params IGnssParser[] parsers)
        {
            DataStream = stream;
            _parsers = parsers;
            foreach (var parser in parsers)
            {
                parser.OnError.Subscribe(_onErrorSubject, _disposeCancel.Token);
                parser.OnMessage.Subscribe(_onMessageSubject, _disposeCancel.Token);
            }
            DataStream.SelectMany(_ => _).Subscribe(OnByteRecv, _disposeCancel.Token);
        }

        private void OnByteRecv(byte data)
        {
            lock (_sync)
            {
                _diag.Speed["rx"].Increment(1);
                try
                {
                    var packetFound = false;
                    for (var index = 0; index < _parsers.Length; index++)
                    {
                        var parser = _parsers[index];
                        if (parser.Read(data))
                        {
                            _diag.Speed[parser.ProtocolId].Increment(1);
                            packetFound = true;
                            break;
                        }
                    }

                    if (packetFound)
                    {
                        foreach (var parser in _parsers)
                        {
                            parser.Reset();
                        }
                    }
                }
                catch (Exception e)
                {
                    _diag.Int["parser err"]++;
                    Debug.Assert(false);
                }
                
            }
        }

        public IDataStream DataStream { get; }

        public IObservable<GnssParserException> OnError => _onErrorSubject;
        public IObservable<GnssMessageBase> OnMessage => _onMessageSubject;

        public void Dispose()
        {
            _portSubscribe?.Dispose();
            _diag?.Dispose();
            _disposeCancel.Cancel(false);
            _disposeCancel.Dispose();
            _onErrorSubject?.Dispose();
            _onMessageSubject?.Dispose();
            if (_disposeDataStream)
            {
                (DataStream as IDisposable)?.Dispose();
            }
        }
    }
}