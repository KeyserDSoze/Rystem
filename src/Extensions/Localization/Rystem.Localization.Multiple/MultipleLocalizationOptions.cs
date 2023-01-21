namespace Microsoft.Extensions.Localization;

/// <summary>
/// Provides programmatic configuration for localization.
/// </summary>
public class MultipleLocalizationOptions : LocalizationOptions
{
    public string FullNameAssembly { get; set; } = null!;
}
