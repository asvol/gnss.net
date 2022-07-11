using System;

namespace Asv.Gnss.V2
{
    public interface IGnssMessageParser:IDisposable
    {
        string ProtocolId { get; }
        bool Read(byte data);
        void Reset();
        IObservable<GnssParserException> OnError { get; }
        IObservable<IGnssMessageBase> OnMessage { get; }
    }
}