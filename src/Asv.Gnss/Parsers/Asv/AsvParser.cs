using System;
using Asv.Tools;

namespace Asv.Gnss
{
    public class AsvParser:GnssParserWithMessagesBase<AsvMessageBase,ushort>
    {
        public const ushort MaxMessageSize = 1012/*DATA*/ + 10/*HEADER*/ + 2/*CRC16*/;
        public const byte Sync1 = 0xAA;
        public const byte Sync2 = 0x44;

        private enum State
        {
            Sync1,
            Sync2,
            MessageLength,
            Message,
        }

        private State _state;
        private int _bufferIndex;
        private readonly byte[] _buffer = new byte[MaxMessageSize];
        private int _stopIndex;

        public const string GnssProtocolId = "Asv";

        public override string ProtocolId => GnssProtocolId;

        public AsvParser(IDiagnostic diag):this(diag[GnssProtocolId])
        {

        }

        public AsvParser(IDiagnosticSource diagSource) : base(diagSource)
        {

        }

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
                        if (_stopIndex >= _buffer.Length)
                        {
                            // wrong length
                            _state = State.Sync1;
                        }
                        else
                        {
                            _state = State.Message;
                        }
                        
                    }
                    break;
                case State.Message:
                    _buffer[_bufferIndex++] = data;
                    if (_bufferIndex == _stopIndex)
                    {
                        var crc = BitConverter.ToUInt16(_buffer, _stopIndex - 2);
                        var calcCrc = SbfCrc16.checksum(_buffer, 0, _stopIndex - 2);
                        if (calcCrc == crc)
                        {
                            var msgId = BitConverter.ToUInt16(_buffer, 8);
                            ParsePacket(msgId, _buffer);
                        }
                        else
                        {
                            Diag.Int["crc err"]++;
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
    }
}