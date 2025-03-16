namespace Rystem.Localization
{
    public sealed class FormattedString
    {
        public required string Value { get; init; }
        public string this[params object[] parameters]
        {
            get => string.Format(Value, parameters);
        }
        public static implicit operator FormattedString(string formattableString) => new() { Value = formattableString };
    }
}
