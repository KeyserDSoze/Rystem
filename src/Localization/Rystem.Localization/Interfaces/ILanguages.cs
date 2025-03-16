namespace Rystem.Localization
{
    public interface ILanguages<T>
    {
        RystemLocalizationFiles<T> Localizer { get; }
    }
}
