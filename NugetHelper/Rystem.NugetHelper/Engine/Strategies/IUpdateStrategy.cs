namespace Rystem.NugetHelper.Engine
{
    internal interface IUpdateStrategy
    {
        int Value { get; }
        string Label { get; }
        Package GetStrategy();
    }
}
