using Rystem.Web.Components.Contents.DataTable;
using Rystem.Web.Components.Customization;

namespace Rystem.Web.Components.Contents
{
    public sealed class DataTableSettings<T, TKey>
        where TKey : notnull
    {
        public string CssClass { get; set; } = string.Empty;
        public Dictionary<TKey, T>? Items { get; set; }
        public Func<PaginationState, FilterWrapper<T>, Task<(Dictionary<TKey, T> Items, int Count)>>? ItemsSelector { get; set; }
        public ColorType Color { get; set; }
        public SizeType Size { get; set; }
        public bool Striped { get; set; }
        public bool Sticky { get; set; }
        public BorderType Bordered { get; set; }
        public BreakpointType Responsive { get; set; }
        public bool Hover { get; set; }
    }
}
