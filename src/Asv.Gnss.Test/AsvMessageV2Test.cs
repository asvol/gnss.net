using System;
using System.Collections.Generic;
using Asv.Gnss.V2;
using Asv.Tools;
using Asv.Tools.Test;
using Xunit;
using Xunit.Abstractions;

namespace Asv.Gnss.Test
{
    public class AsvMessageV2Test
    {
        private readonly ITestOutputHelper _output;

        public AsvMessageV2Test(ITestOutputHelper output)
        {
            _output = output;
        }


        [Fact]
        public void MessagesAndParserTest()
        {
            var seed = new Random().Next();
            var r = new Random(seed);
            _output.WriteLine("RANDOM SEED:{0}", seed);
            var parser = new AsvMessageParser(new KeyValuePair<string, object>("test", "test")).RegisterDefaultMessages();
            SpanTestHelper.SerializeDeserializeTestBegin(_output.WriteLine);
            foreach (var func in AsvMessageParserFactory.DefaultMessages)
            {
                var message = func();
                message.Randomize(r);
                SpanTestHelper.TestType(message, func, _output.WriteLine);
                ParserTestHelper.TestParser(parser, message, r);
            }


            
            

            
           
        }

        
    }
}