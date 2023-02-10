using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class SizeClassBuilder<T> : ClassBuilder
    {
        public T S1
        {
            get
            {
                _stringBuilder.Append("-1");
                return (T)(object)this;
            }
        }
        public SizeClassBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
        }
    }
    public class BreakPointClassBuilder<T> : ClassBuilder
    {
        public T Small
        {
            get
            {
                _stringBuilder.Append("-sm");
                return (T)(object)this;
            }
        }
        public T Medium
        {
            get
            {
                _stringBuilder.Append("-md");
                return (T)(object)this;
            }
        }
        public T Large
        {
            get
            {
                _stringBuilder.Append("-lg");
                return (T)(object)this;
            }
        }
        public T ExtraLarge
        {
            get
            {
                _stringBuilder.Append("-xl");
                return (T)(object)this;
            }
        }
        public BreakPointClassBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
        }
    }
    public class ClassBuilder
    {
        public static ClassBuilder Style => new(new());
        private protected StringBuilder _stringBuilder;
        private protected ClassBuilder(StringBuilder stringBuilder)
        {
            _stringBuilder = stringBuilder;
        }
        public ContainerBuilder Container
        {
            get
            {
                _stringBuilder.Append(" container");
                return new(_stringBuilder);
            }
        }
        public ColumnBuilder Col
        {
            get
            {
                _stringBuilder.Append(" col");
                return new(_stringBuilder);
            }
        }
        public override string ToString() => _stringBuilder.ToString();
    }
    public sealed class ContainerBuilder : BreakPointClassBuilder<ClassBuilder>
    {
        public ContainerBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
        }
    }
    public sealed class ColumnBuilder : BreakPointClassBuilder<SizeClassBuilder<ClassBuilder>>
    {
        public ColumnBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
        }
    }
    public sealed class Grid
    {
        public string Value { get; }
        private Grid(string value)
        {
            Value = value;
        }
        public static Grid Row { get; } = new("row");
        public static Grid Col { get; } = new("col");
        public static Grid Small { get; } = new("col-sm");
        public static Grid Medium { get; } = new("col-md");
        public static Grid Large { get; } = new("col-lg");
        public static Grid ExtraLarge { get; } = new("col-xl");
        public static Grid ExtraExtraLarge { get; } = new("col-xxl");
    }
    public enum BreakpointType
    {
        None,
        Small,
        Medium,
        Large,
        ExtraLarge,
        ExtraExtraLarge,
        Every
    }
    public static class BreakpointExtensions
    {
        public static string ToBoostrapBreakpoint(this BreakpointType type)
        {
            switch (type)
            {
                case BreakpointType.Small:
                    return "-sm";
                case BreakpointType.Medium:
                    return "-md";
                case BreakpointType.Large:
                    return "-lg";
                case BreakpointType.ExtraLarge:
                    return "-xl";
                case BreakpointType.ExtraExtraLarge:
                    return "-xxl";
                default:
                    return string.Empty;
            }
        }
    }
}
