using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Asv.Gnss.V2
{
    public abstract class GnssMessageBase<TMsgId> : IGnssMessageBase
    {
        public GnssMessageBase()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            Debug.Assert(Name != null, nameof(Name) + " != null");
            // ReSharper disable once VirtualMemberCallInConstructor
            Debug.Assert(MessageId != null, nameof(MessageId) + " != null");
            // ReSharper disable once VirtualMemberCallInConstructor
            // ReSharper disable once VirtualMemberCallInConstructor
            MetricTag = new KeyValuePair<string, object>("MSG", $"{Name}[ID={MessageId}]");
        }
        
        /// <summary>
        /// This is for custom use (like routing, etc...)
        /// This field not serialize\deserialize
        /// </summary>
        public object Tag { get; set; }

        public KeyValuePair<string,object> MetricTag { get; }

        public abstract string ProtocolId { get; }
        public abstract TMsgId MessageId { get; }
        public abstract string Name { get; }

        public abstract void Deserialize(ref ReadOnlySpan<byte> buffer);
        public abstract void Serialize(ref Span<byte> buffer);
        public abstract int GetByteSize();

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}