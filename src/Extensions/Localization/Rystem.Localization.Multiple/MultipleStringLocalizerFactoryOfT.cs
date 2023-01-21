using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Localization;

internal sealed class MultipleStringLocalizerFactory : IMultipleStringLocalizerFactory
{
    private readonly Dictionary<string, IStringLocalizerFactory> _localizers = new();

    public MultipleStringLocalizerFactory(IEnumerable<MultipleLocalizationOptions> localizationOptions,
        ILoggerFactory loggerFactory)
    {
        if (localizationOptions == null)
        {
            throw new ArgumentNullException(nameof(localizationOptions));
        }
        foreach (var options in localizationOptions)
        {
            _localizers.Add(options.FullNameAssembly, new ResourceManagerStringLocalizerFactory(new MultipleOptions(options), loggerFactory));
        }
    }
    public IStringLocalizerFactory GetFactory(Type type)
    {
        var assemblyName = type.Assembly.GetName().Name;
        if (_localizers.ContainsKey(assemblyName!))
            return _localizers[assemblyName!];
        return _localizers.First().Value;
    }
}
