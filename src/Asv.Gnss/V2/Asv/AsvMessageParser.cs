using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Asv.Gnss.V2
{
    public class AsvMessageParser : GnssMessageParserBase<AsvMessageBase, ushort>
    {
        #region Metrics 

        protected static readonly Counter<int> MeterCrcError = Meter.CreateCounter<int>("crc-error", "pkt", "Crc calculation error");

        #endregion

        public const string GnssProtocolId = "Asv";
        public const ushort MaxMessageSize = 1012/*DATA*/ + 10/*HEADER*/ + 2/*CRC16*/;
        public const byte Sync1 = 0xAA;
        public const byte Sync2 = 0x44;

        public override string ProtocolId => GnssProtocolId;

        public AsvMessageParser(KeyValuePair<string, object> metricTag) : base(metricTag)
        {

        }


        private readonly byte[] _buffer = new byte[MaxMessageSize];
        private State _state;
        private int _bufferIndex;
        private int _stopIndex;

        public override bool Read(byte data)
        {
            switch (_state)
            {
                case State.Sync1:
                    if (data != Sync1) return false;
                    _bufferIndex = 0;
                    _buffer[_bufferIndex++] = Sync1;
                    _state = State.Sync2;
                    break;
                case State.Sync2:
                    if (data != Sync2)
                    {
                        _state = State.Sync1;
                    }
                    else
                    {
                        _state = State.MessageLength;
                        _buffer[_bufferIndex++] = Sync2;
                    }
                    break;
                case State.MessageLength:
                    _buffer[_bufferIndex++] = data;
                    if (_bufferIndex == 4)
                    {
                        _stopIndex = BitConverter.ToUInt16(_buffer, 2) + 12; // 10 header + 2 crc = 12
                        _state = _stopIndex >= _buffer.Length ? State.Sync1 : State.Message;
                    }
                    break;
                case State.Message:
                    _buffer[_bufferIndex++] = data;
                    if (_bufferIndex == _stopIndex)
                    {
                        var crc = BitConverter.ToUInt16(_buffer, _stopIndex - 2);
                        var calcCrc = SbfCrc16.Calc(_buffer, 0, _stopIndex - 2);
                        if (calcCrc == crc)
                        {
                            var msgId = BitConverter.ToUInt16(_buffer, 8);
                            var span = new ReadOnlySpan<byte>(_buffer,0, _stopIndex);
                            ParsePacket(msgId, ref span);
                            if (span.IsEmpty == false)
                            {
                                //not all data readed
                                if (Debugger.IsAttached)
                                {
                                    Debugger.Break();
                                }
                            }
                        }
                        else
                        {
                            MeterCrcError.Add(1,MetricTag,ProtocolMetricTag);
                            InternalOnError(new GnssParserException(ProtocolId, $"{ProtocolId} crc16 error"));
                        }
                        _state = State.Sync1;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return false;
        }

        public override void Reset()
        {
            _state = State.Sync1;
        }

        private enum State
        {
            Sync1,
            Sync2,
            MessageLength,
            Message,
        }

        
    }
}