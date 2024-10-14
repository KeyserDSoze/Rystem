using System.Text.RegularExpressions;

namespace Rystem.OpenAi.Actors
{
    public interface IScenePathBuilder
    {
        IScenePathBuilder Map(Regex regex);
        IScenePathBuilder Map(string startsWith);
    }
}
