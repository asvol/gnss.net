using Asv.Tools;

namespace Asv.Gnss.V2
{
    public interface IGnssMessageBase: ISizedSpanSerializable
    {
        /// <summary>
        /// This is for custom use (like routing, etc...)
        /// This field not serialize\deserialize
        /// </summary>
        object Tag { get; set; }
        string ProtocolId { get; }
    }
}