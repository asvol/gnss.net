﻿using System;
using System.Collections.Generic;

namespace Asv.Gnss
{
    public abstract class GnssParserWithMessagesBase<TMessage, TMsgId> : GnssParserBase where TMessage : GnssMessageBaseWithId<TMsgId>
    {
        private readonly Dictionary<TMsgId, Func<TMessage>> _dict = new Dictionary<TMsgId, Func<TMessage>>();
        private readonly IDiagnosticSource _diag;

        protected GnssParserWithMessagesBase(IDiagnostic diag)
        {
            _diag = diag[ProtocolId];
        }

        public void Register(Func<TMessage> factory)
        {
            var testPckt = factory();
            _dict.Add(testPckt.MessageId, factory);
        }

        protected IDiagnosticSource Diag => _diag;

        protected void ParsePacket(TMsgId id, byte[] data)
        {
            _diag.Speed[id.ToString()].Increment(1);
            if (_dict.TryGetValue(id, out var factory) == false)
            {
                _diag.Int["unk err"]++;
                InternalOnError(new GnssParserException(ProtocolId, $"Unknown {ProtocolId} packet message number [MSG={id}]"));
                return;
            }

            var message = factory();

            try
            {
                message.Deserialize(data, 0);
            }
            catch (Exception e)
            {
                _diag.Int["parse err"]++;
                InternalOnError(new GnssParserException(ProtocolId, $"Parse {ProtocolId} packet error [MSG={id}]", e));
            }

            try
            {
                InternalOnMessage(message);
            }
            catch (Exception e)
            {
                _diag.Int["pub err"]++;
                InternalOnError(new GnssParserException(ProtocolId, $"Parse {ProtocolId} packet error [MSG={id}]", e));
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _diag?.Dispose();
        }
    }
}