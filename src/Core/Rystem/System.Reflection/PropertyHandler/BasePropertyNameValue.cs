using System.Text;

namespace System.Reflection
{
    public sealed class BasePropertyNameValue
    {
        private readonly StringBuilder _navigationPathBuilder = new();
        public string NavigationPath { get; private set; } = string.Empty;
        public string CompleteName => $"{_navigationPathBuilder}{(_index != -1 ? $"[{_index}]" : string.Empty)}.{Name}";
        public string? Name { get; private set; }
        public object? Value { get; set; }
        private int _index = -1;
        public void AddName(string name)
        {
            if (_navigationPathBuilder.Length > 0)
                _navigationPathBuilder.Append('.');
            _navigationPathBuilder.Append(Name ?? string.Empty);
            Name = name;
            NavigationPath = _navigationPathBuilder.ToString();
        }
        public void AddIndex(int index)
        {
            if (_index != -1)
                _navigationPathBuilder.Append($"[{_index}]");
            _index = index;
            //Name = $"{Name}[{index}]";
        }
    }
}
