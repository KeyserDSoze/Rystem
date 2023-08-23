namespace RepositoryFramework.Wasm.Services
{
    public interface ISony
    {

    }
    public interface IHelper
    {

    }
    public interface IMilly
    {

    }
    internal sealed class Milly : IMilly { }
    public class Sony : ISony
    {
        private readonly IMilly _milly;
        private readonly IHelper? _helper;
        public Sony(IMilly milly, IHelper? helper = null)
        {
            _milly = milly;
            _helper = helper;
        }
    }
}
