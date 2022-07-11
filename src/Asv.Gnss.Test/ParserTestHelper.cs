﻿using System;
using System.Reactive.Linq;
using Asv.Gnss.V2;
using DeepEqual.Syntax;
using Xunit;

namespace Asv.Gnss.Test
{
    public static class ParserTestHelper
    {
        
        public static void TestParser<TMessage>(IGnssMessageParser parser,TMessage message, Random r)
            where TMessage: IGnssMessageBase
        {
            var arr = new byte[message.GetByteSize()];
            var span = new Span<byte>(arr);
            message.Serialize(ref span);

            var randomBegin = new byte[r.Next(0, 256)];
            r.NextBytes(randomBegin);

            var parsedMessage = default(TMessage);
            parser.OnMessage.Where(_ => _.ProtocolId == message.ProtocolId).Cast<TMessage>()
                .Subscribe(_ => parsedMessage = _);

            parser.Reset();
            foreach (var b in randomBegin)
            {
                parser.Read(b);
            }

            foreach (var b in arr)
            {
                parser.Read(b);
            }

            Assert.NotNull(parsedMessage);
            message.ShouldDeepEqual(parsedMessage);
        }
    }
}