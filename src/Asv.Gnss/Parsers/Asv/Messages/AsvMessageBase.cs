using System;
using System.IO;
using System.Text;

namespace Asv.Gnss
{
    
    public abstract class AsvMessageBase:GnssMessageBaseWithId<ushort>
    {
        public override string ProtocolId => AsvParser.GnssProtocolId;

        public override int GetMaxByteSize()
        {
            return AsvParser.MaxMessageSize;
        }

        public override uint Serialize(byte[] buffer, uint offsetBits)
        {
            var startIndex = (int)(offsetBits / 8);
            var length = (ushort)InternalSerialize(buffer, startIndex + 10);
            using (var strm = new BinaryWriter(new MemoryStream(buffer, startIndex, buffer.Length - startIndex), Encoding.ASCII,false))
            {
                strm.Write(AsvParser.Sync1);
                strm.Write(AsvParser.Sync2);
                strm.Write(length);
                strm.Write(Sequence);
                strm.Write(SenderId);
                strm.Write(TargetId);
                strm.Write(MessageId);
                strm.BaseStream.Position = length + 10;
                var crc = SbfCrc16.Calc(buffer, startIndex, length + 10);
                strm.Write(crc);
            }

            return (uint) (length + 12 /* header 10 + crc 2*/) * 8;
        }

        protected abstract int InternalSerialize(byte[] buffer, int offsetInBytes);

        public ushort Sequence { get; set; }
        public byte TargetId { get; set; }
        public byte SenderId { get; set; }

        public override uint Deserialize(byte[] buffer, uint offsetBits)
        {
            var startIndex = (int)(offsetBits / 8);
            var msgLength = BitConverter.ToUInt16(buffer, startIndex + 2);
            Sequence = BitConverter.ToUInt16(buffer, startIndex + 4);
            SenderId = buffer[startIndex + 6];
            TargetId = buffer[startIndex + 7];
            var msgId = BitConverter.ToUInt16(buffer, startIndex + 8);
            if (MessageId != msgId) throw new Exception($"Message id not equals. Want '{MessageId}. Got '{msgId}''");
            var bytes = InternalDeserialize(buffer, startIndex + 10, msgLength);
            return (uint) ((bytes + 12/* header 10 + crc 2*/) * 8);
        }

        protected abstract int InternalDeserialize(byte[] buffer, int offsetInBytes, int length);
    }

    
}