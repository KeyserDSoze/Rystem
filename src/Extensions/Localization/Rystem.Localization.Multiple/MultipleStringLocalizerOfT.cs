namespace Microsoft.Extensions.Localization;

/// <summary>
/// Provides strings for <typeparamref name="TResourceSource"/>.
/// </summary>
/// <typeparam name="TResourceSource">The <see cref="Type"/> to provide strings for.</typeparam>
public class MultipleStringLocalizer<TResourceSource> : StringLocalizer<TResourceSource>
{
    public MultipleStringLocalizer(IMultipleStringLocalizerFactory factoryOfFactory) : base(factoryOfFactory.GetFactory(typeof(TResourceSource)))
    {
    }
}
