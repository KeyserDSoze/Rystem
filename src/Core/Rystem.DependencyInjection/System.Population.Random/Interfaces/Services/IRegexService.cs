using System;

namespace System.Population.Random
{
    /// <summary>
    /// Interface that helps the random generation of object based on regular expressions during random population.
    /// </summary>
    public interface IRegexService
    {
        dynamic GetRandomValue(Type type, string[] pattern);
    }
}
