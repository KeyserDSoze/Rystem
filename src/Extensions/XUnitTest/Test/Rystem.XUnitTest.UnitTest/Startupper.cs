using Xunit.Abstractions;
using Xunit.Sdk;

//[assembly: TestFramework("MyNamespace.Startupper", "Rystem.XUnitTest.UnitTest")]

namespace MyNamespace
{
    public class Startupper : XunitTestFramework
    {
        public Startupper(IMessageSink messageSink)
          : base(messageSink)
        {
            // Place initialization code here
        }

        public new void Dispose()
        {
            // Place tear down code here
            base.Dispose();
        }
    }
}
