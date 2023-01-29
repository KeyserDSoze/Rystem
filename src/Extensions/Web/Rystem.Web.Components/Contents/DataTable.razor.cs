using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Csv;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Rystem.Web.Components.Contents.DataTable;
using Rystem.Web.Components.Customization;
using Rystem.Web.Components.Services;

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
    public partial class DataTable<T, TKey>
        where TKey : notnull
    {
        [Parameter]
        public required DataTableSettings<T, TKey> Settings { get; set; }
        [Parameter]
        public PaginationState Pagination { get; set; } = new();
        [Inject]
        public required IJSRuntime JSRuntime { get; set; }
        [Inject]
        public ICopyService? Copy { get; set; }
        [Inject]
        public IDialogService? DialogService { get; set; }
        [Inject]
        public ILoaderService? LoaderService { get; set; }
        [Inject]
        public required NavigationManager NavigationManager { get; set; }
        private const string DefaultCssStyle = "table{0}{1}{2}{3}{4}{5}{6} {7}";
        private string _cssStyle = string.Empty;
        public required TypeShowcase Showcase { get; set; }
        private readonly Dictionary<string, ColumnOptions> _columns = new();
        private readonly FilterWrapper<T> _filterWrapper = new();
        private Dictionary<string, PropertyUiSettings> _propertiesRetrieved;
        private bool _allSelected;
        protected override void OnInitialized()
        {
            Showcase = typeof(T).ToShowcase();
            foreach (var property in Showcase.FlatProperties)
            {
                _columns.Add(property.NavigationPath, new ColumnOptions
                {
                    Type = property.Self.PropertyType,
                    Order = OrderingType.None,
                    IsActive = true,
                    Label = property.NavigationPath,
                    Value = property.NavigationPath
                });
            }
            base.OnInitialized();
        }
        protected override void OnParametersSet()
        {
            _cssStyle = string.Format(DefaultCssStyle,
                Settings.Striped ? " table-striped" : string.Empty,
                Settings.Sticky ? " table-sticky" : string.Empty,
                Settings.Hover ? " table-hover" : string.Empty,
                $"table-{Settings.Color.ToString().ToLower()}",
                Settings.Bordered == BorderType.Everything ? " table-bordered" : (Settings.Bordered == BorderType.None ? " table-borderless" : string.Empty),
                $"table{Settings.Size.ToBootstrapSize()}",
                Settings.Responsive == BreakpointType.None ? string.Empty : $"table-responsive{Settings.Responsive.ToBoostrapBreakpoint()}",
                Settings.CssClass);
            base.OnParametersSet();
        }
        protected override async Task OnParametersSetAsync()
        {
            await OnReadDataAsync().NoContext();
            await base.OnParametersSetAsync().NoContext();
        }
        private string GetEditUri(TKey key)
            => s_editUri != null ? string.Format(s_editUri, key.ToBase64()) : string.Empty;
        private string? _lastQueryKey;
        private Dictionary<TKey, T> _items;
        private async ValueTask OnReadDataAsync()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.Append($"{Pagination.CurrentPageIndex}_{Pagination.ItemsPerPage}_");
            stringBuilder.Append(string.Join('_', _filterWrapper.Search.GetExpressions()));
            stringBuilder.Append(string.Join('_', _filterWrapper.Order.GetExpressions()));
            var queryKey = stringBuilder.ToString();
            if (_lastQueryKey != queryKey)
            {
                LoaderService?.Show();
                _lastQueryKey = queryKey;
                if (Settings.ItemsSelector != null)
                {
                    var response = await Settings.ItemsSelector.Invoke(Pagination, _filterWrapper);
                    _items = response.Items;
                    Pagination.SetItemsPerPage(response.Count);
                }
                else if (Settings.Items != null)
                {
                    var newDictionary = new Dictionary<TKey, T>();
                    foreach (var item in _filterWrapper
                        .Apply(Settings.Items.Select(x => x.Value))
                        .Skip(Pagination.SkipValue)
                        .Take(Pagination.ItemsPerPage))
                    {
                        var entity = Settings.Items.FirstOrDefault(x => x.Value?.Equals(item) == true);
                        if (!newDictionary.ContainsKey(entity.Key))
                            newDictionary.Add(entity.Key, entity.Value);
                    }
                    _items = newDictionary;
                }
                _ = InvokeAsync(() => StateHasChanged());
                LoaderService?.Hide();
            }
        }
        private string? _selectedPageKey = "0";
        public void GoToPage(int page)
        {
            if (page < 0)
                page = 0;
            else if (page > Pagination.LastPageIndex)
                page = Pagination.LastPageIndex.Value;
            Pagination.CurrentPageIndex = page;
            _selectedPageKey = Pagination.CurrentPageIndex.ToString();
            _ = OnReadDataAsync();
        }
        private string? _selectedItemsForPageKey = "0";
        private void ChangeItemsPerPage(int itemsPerPage)
        {
            Pagination.SetItemsPerPage(itemsPerPage);
            _selectedItemsForPageKey = itemsPerPage.ToString();
            GoToPage(0);
        }
        private void OrderBy(BaseProperty baseProperty)
        {
            var order = (OrderingType)(((int)_columns[baseProperty.NavigationPath].Order + 1) % 3);
            _columns[baseProperty.NavigationPath].Order = order;
            if (order == OrderingType.None)
                _filterWrapper.Order.Remove(baseProperty);
            else if (order == OrderingType.Ascending)
                _filterWrapper.Order.Add(baseProperty);
            else
                _filterWrapper.Order.Add(baseProperty, true);
            GoToPage(0);
        }
        private async Task ShowMoreValuesAsync(Entity<T, TKey>? entity, BaseProperty property)
        {
            var retrieve = Try.WithDefaultOnCatch(() => property.Value(entity, null));
            if (retrieve.Exception == null && retrieve.Entity is IEnumerable enumerable && enumerable.GetEnumerator().MoveNext())
            {
                _ = await DialogService.OpenAsync<Visualizer>(property.GetFurtherProperty().Title,
                    new Dictionary<string, object>
                    {
                        { Constant.Entity, retrieve.Entity },
                    }, new DialogOptions
                    {
                        Width = Constant.DialogWidth
                    });
            }
        }
        private IEnumerable<LabelValueDropdownItem> GetColumns()
        {
            foreach (var column in _columns)
                yield return new LabelValueDropdownItem
                {
                    Id = column.Key,
                    Value = column.Value,
                    Label = column.Value.Label,
                };
        }
        private IEnumerable<string> GetSelectedColumns()
        {
            foreach (var column in _columns.Where(x => x.Value.IsActive))
                yield return column.Key;
        }
        private ValueTask UpdateColumnsVisibility(IEnumerable<LabelValueDropdownItem> keys)
        {
            foreach (var item in _columns)
                item.Value.IsActive = false;
            foreach (var key in keys)
                _columns[key.Id].IsActive = true;
            _ = InvokeAsync(() => StateHasChanged());
            return ValueTask.CompletedTask;
        }
        private IEnumerable<LabelValueDropdownItem> GetPages()
        {
            for (var i = 0; i <= Pagination.LastPageIndex; i++)
            {
                yield return new LabelValueDropdownItem
                {
                    Label = LocalizationHandler.Get(LanguageLabel.OfPages, i + 1, Pagination.LastPageIndex + 1),
                    Id = i.ToString(),
                    Value = i,
                };
            }
        }

        private IEnumerable<LabelValueDropdownItem> GetPaging()
        {
            for (var i = 10; i < Pagination.TotalItemCount; i *= 2)
            {
                yield return new LabelValueDropdownItem
                {
                    Label = LocalizationHandler.Get(LanguageLabel.PerPage, i),
                    Id = i.ToString(),
                    Value = i,
                };
            }
            yield return new LabelValueDropdownItem
            {
                Label = LocalizationHandler.Get(LanguageLabel.All, Pagination.TotalItemCount),
                Id = Pagination.TotalItemCount.ToString(),
                Value = Pagination.TotalItemCount.Value,
            };
        }
        private PropertyUiSettings? GetPropertySettings(BaseProperty property)
        {
            var propertyUiSettings = _propertiesRetrieved != null && _propertiesRetrieved.ContainsKey(property.NavigationPath) ? _propertiesRetrieved[property.NavigationPath] : null;
            return propertyUiSettings;
        }
        public void Search()
        {
            GoToPage(0);
        }
        private void NavigateTo(string uri)
        {
            LoaderService?.Show();
            NavigationManager.NavigateTo(uri);
        }
        private Dictionary<TKey, bool> _selectedKeys = new();
        private void AddOrRemoveItemFromList(bool check, TKey key)
        {
            _selectedKeys[key] = check;
        }
        private void AddOrRemoveItemFromListAllKeys(ChangeEventArgs args, IEnumerable<TKey> keys)
        {
            var isSelected = (args.Value is bool check && check);
            foreach (var key in keys)
                AddOrRemoveItemFromList(isSelected, key);
            _allSelected = true;
            _ = InvokeAsync(() => StateHasChanged());
        }

        private const string CsvContentType = "text/csv";
        private async ValueTask DownloadAsCsvAsync()
        {
            var fileName = $"{typeof(T).Name}_{_lastQueryKey}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            var file = Encoding.UTF8.GetBytes(_items.ToCsv());
            await JSRuntime.InvokeVoidAsync("BlazorDownloadFile", fileName, CsvContentType, file);
        }
        private string CopyAsCsv()
            => _items!.ToCsv();
        private const string RemoveIcon = "remove";
        private const string ArrowDropDownIcon = "arrow_drop_down";
        private const string ArrowDropUpIcon = "arrow_drop_up";
        private string GetRightOrderingIcon(OrderingType order)
        {
            if (order == OrderingType.None)
            {
                return RemoveIcon;
            }
            else if (order == OrderingType.Ascending)
            {
                return ArrowDropDownIcon;
            }
            else if (order == OrderingType.Descending)
            {
                return ArrowDropUpIcon;
            }
            return string.Empty;
        }
        private string Translate(string value)
            => LocalizationHandler.Get<T>(value);
    }
}
