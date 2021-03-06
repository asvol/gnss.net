using System;
using System.Reactive;
using System.Reactive.Subjects;

namespace Asv.Gnss
{
    public interface IGnssParser:IDisposable
    {
        string ProtocolId { get; }
        bool Read(byte data);
        void Reset();
        IObservable<GnssParserException> OnError { get; }
        IObservable<GnssMessageBase> OnMessage { get; }
    }
}