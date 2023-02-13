using System.Text;

namespace Rystem.Web.Components.Customization
{
    public interface ICssClassBuilder
    {
        StringBuilder StringBuilder { get; init; }
        string Prefix { get; init; }
    }
    public class SizeClassBuilder<T> : DefaultClassBuilder
        where T : ICssClassBuilder, new()
    {
        public T S1
        {
            get
            {
                StringBuilder.Append("-1");
                return CreateNew<T>();
            }
        }
        public T S2
        {
            get
            {
                StringBuilder.Append("-2");
                return CreateNew<T>();
            }
        }
    }
    public class BreakPointClassBuilder<T> : DefaultClassBuilder
        where T : ICssClassBuilder, new()
    {
        public T Default
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Prefix))
                    StringBuilder.Append(Prefix);
                return CreateNew<T>();
            }
        }
        public T Small
        {
            get
            {
                StringBuilder.Append($"{Prefix}-sm");
                return CreateNew<T>();
            }
        }
        public T Medium
        {
            get
            {
                StringBuilder.Append($"{Prefix}-md");
                return CreateNew<T>();
            }
        }
        public T Large
        {
            get
            {
                StringBuilder.Append($"{Prefix}-lg");
                return CreateNew<T>();
            }
        }
        public T ExtraLarge
        {
            get
            {
                StringBuilder.Append($"{Prefix}-xl");
                return CreateNew<T>();
            }
        }
        public T ExtraExtraLarge
        {
            get
            {
                StringBuilder.Append($"{Prefix}-xxl");
                return CreateNew<T>();
            }
        }
    }
    public class DefaultClassBuilder : ICssClassBuilder
    {
        public StringBuilder StringBuilder { get; init; } = null!;
        public string Prefix { get; init; } = string.Empty;
        private protected T CreateNew<T>()
            where T : ICssClassBuilder, new()
            => new() { StringBuilder = StringBuilder, Prefix = Prefix };
        public override string ToString() => StringBuilder.ToString();
    }
    public class ClassBuilder : DefaultClassBuilder
    {
        public static ClassBuilder Style => new() { StringBuilder = new() };
        public ContainerBuilder Container
        {
            get
            {
                StringBuilder.Append(" container");
                return CreateNew<ContainerBuilder>();
            }
        }
        public ColumnBuilder Col
        {
            get
            {
                StringBuilder.Append(" col");
                return CreateNew<ColumnBuilder>();
            }
        }
        public RowBuilder Row
        {
            get
            {
                var style = RowBuilder.Style;
                RowBuilder.Style.StringBuilder.Append(" row");
                return style;
            }
        }
    }
    public sealed class ContainerBuilder : BreakPointClassBuilder<ClassBuilder>
    {
    }
    public sealed class ColumnBuilder : BreakPointClassBuilder<SizeClassBuilder<ClassBuilder>>
    {
    }
    public sealed class RowBuilder : BreakPointClassBuilder<SizeClassBuilder<RowBuilder>>
    {
        private const string RowPrefix = " row-cols";
        public static RowBuilder Style => new() { StringBuilder = new(), Prefix = RowPrefix };
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
}
