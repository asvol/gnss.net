﻿using System;
using System.Collections.Generic;
using Asv.Tools;

namespace Asv.Gnss
{
    public class SbfBinaryParser: GnssParserBase
    {
        public const string GnssProtocolId = "SBF";
        public const int MaxPacketSize = 8192;
        private State _state;
        private readonly byte[] _buffer = new byte[MaxPacketSize];
        private int _bufferIndex = 0;
        private readonly IDiagnosticSource _diag;
        public override string ProtocolId => GnssProtocolId;
        private readonly Dictionary<ushort, Func<SbfPacketBase>> _dict = new Dictionary<ushort, Func<SbfPacketBase>>();
        private ushort _crc;
        private ushort _msgId;
        private ushort _length;

        public SbfBinaryParser(IDiagnostic diag):this(diag[GnssProtocolId])
        {
            
        }

        public SbfBinaryParser(IDiagnosticSource diag)
        {
            _diag = diag;
        }

        private enum State
        {
            Sync1,
            Sync2,
            CrcAndIdAndLength,
            Message
        }

        public override bool Read(byte data)
        {
            switch (_state)
            {
                case State.Sync1:
                    if (data != 0x24) return false;
                    _bufferIndex = 0;
                    _buffer[_bufferIndex++] = 0x24;
                    _state = State.Sync2;
                    break;
                case State.Sync2:
                    if (data != 0x40)
                    {
                        _state = State.Sync1;
                    }
                    else
                    {
                        _state = State.CrcAndIdAndLength;
                        _buffer[_bufferIndex++] = 0x40;
                    }
                    break;
                case State.CrcAndIdAndLength:
                    if (_bufferIndex >= _buffer.Length)
                    {
                        _state = State.Sync1;
                        return false;
                    }
                    _buffer[_bufferIndex++] = data;
                    if (_bufferIndex == 8)
                    {
                        _crc = BitConverter.ToUInt16(_buffer, 2);
                        _msgId = BitConverter.ToUInt16(_buffer, 4);
                        _length = BitConverter.ToUInt16(_buffer, 6);
                        _state = State.Message;
                    }
                    break;
                case State.Message:
                    if (_bufferIndex >= _buffer.Length)
                    {
                        _state = State.Sync1;
                        return false;
                    }
                    _buffer[_bufferIndex++] = data;
                    if (_bufferIndex == _length)
                    {
                        var calculatedHash = SbfCrc16.Calc(_buffer,4, _length - 4);
                        if (calculatedHash == _crc)
                        {
                            ParsePacket(_buffer);
                            return true;
                        }
                        else
                        {
                            _diag.Int["crc err"]++;
                            InternalOnError(new GnssParserException(ProtocolId, $"SBF crc16 error"));
                        }
                        _state = State.Sync1;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();

            }
            return false;
        }

        public void Register(Func<SbfPacketBase> factory)
        {
            var testPckt = factory();
            _dict.Add(testPckt.MessageId, factory);
        }

        private void ParsePacket(byte[] data)
        {
            var messageType = (ushort)(_msgId & 0x1fff << 0);
            var messageRevision = (ushort)(_msgId >> 13);

            _diag.Rate[$"SBF_{messageType}({messageRevision})"].Increment(1);
            if (_dict.TryGetValue(_msgId, out var factory) == false)
            {
                _diag.Int["unk err"]++;
                InternalOnError(new GnssParserException(ProtocolId, $"Unknown SBF packet message number [MSG={_msgId}]"));
                return;
            }

            var message = factory();

            try
            {
                message.Deserialize(data,0);
            }
            catch (Exception e)
            {
                _diag.Int["parse err"]++;
                InternalOnError(new GnssParserException(ProtocolId, $"Parse SBF packet error [MSG={_msgId}]", e));
                return;
            }

            try
            {
                InternalOnMessage(message);
            }
            catch (Exception e)
            {
                _diag.Int["pub err"]++;
                InternalOnError(new GnssParserException(ProtocolId, $"Parse SBF packet error [MSG={_msgId}]", e));
            }
        }

        public override void Reset()
        {
            _state = State.Sync1;
        }
    }
}