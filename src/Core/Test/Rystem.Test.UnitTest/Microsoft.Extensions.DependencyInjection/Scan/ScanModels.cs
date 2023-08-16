using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Test.UnitTest.Microsoft.Extensions.DependencyInjection.Scan
{
    public interface IAnything
    {
    }
    internal class ScanModels : IAnything, IScannable<IAnything>, ISingletonScannable
    {
    }
}
