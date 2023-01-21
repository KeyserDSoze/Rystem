using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen;

namespace RepositoryFramework.Web.Components.Standard
{
    public partial class Edit<T, TKey>
        where TKey : notnull
    {
        [Parameter]
        public string Key { get; set; } = null!;
        [Parameter]
        public bool EditableKey { get; set; } = true;
        [Parameter]
        public bool DisableEdit { get; set; }
        [Parameter]
        public bool AllowDelete { get; set; }
        [Inject]
        public DialogService DialogService { get; set; }
        [Inject]
        public NotificationService NotificationService { get; set; }
        private TypeShowcase TypeShowcase { get; set; } = null!;
        private readonly EditParametersBearer _parametersBearer = new()
        {
            BaseEntity = null,
        };
        private Entity<T, TKey> _entity;
        private bool _isNew;
        private bool _isRequestedToCreateNew;
        protected override async Task OnParametersSetAsync()
        {
            if (Query != null)
            {
                if (!string.IsNullOrWhiteSpace(Key))
                {
                    var key = Key.FromBase64<TKey>();
                    _entity = new(await Query.GetAsync(key).NoContext(), key);
                }
                else
                    _entity = new();
                if (_entity.Value == null)
                {
                    _entity.Value = typeof(T).CreateWithDefaultConstructorPropertiesAndField<T>();
                    _isNew = true;
                    _isRequestedToCreateNew = true;
                }
                _parametersBearer.PropertiesRetrieved =
                    ServiceProvider?.GetService<IRepositoryPropertyUiMapper<T, TKey>>() is IRepositoryPropertyUiMapper<T, TKey> uiMapper ?
                        await uiMapper.ValuesAsync(ServiceProvider!, _entity).NoContext() : new();
                _parametersBearer.BaseEntity = _entity;
                _parametersBearer.EntityRetrieverByKey = ValueRetrieverByKeyAsync;
                _parametersBearer.BaseTypeShowcase = typeof(Entity<T, TKey>).ToShowcase(IFurtherParameter.Create(Constant.FurtherProperty, x => new FurtherProperty(x)));
                _parametersBearer.DisableEdit = DisableEdit;
                _parametersBearer.StateHasChanged = () => StateHasChanged();
            }
            await base.OnParametersSetAsync().NoContext();
            LoadService.Hide();
        }
        private async Task<object?> ValueRetrieverByKeyAsync(object? key)
        {
            if (key is TKey tKey)
                return await Query.GetAsync(tKey).NoContext();
            return null;
        }
        private async Task SaveAsync(bool withRedirect)
        {
            if (Command != null)
            {
                LoadService.Show();
                var result = _isNew ?
                    await Command.InsertAsync(_entity.Key, _entity.Value).NoContext() :
                    await Command.UpdateAsync(_entity.Key, _entity.Value).NoContext();
                if (result.IsOk && withRedirect)
                    NavigationManager.NavigateTo($"../../../../Repository/{typeof(T).Name}/Query");
                else
                    LoadService.Hide();
                if (!result.IsOk)
                {
                    LoadService.Hide();
                    NotificationService.Notify(new Radzen.NotificationMessage
                    {
                        Duration = 4_000,
                        CloseOnClick = true,
                        Severity = Radzen.NotificationSeverity.Error,
                        Summary = "Saving error",
                        Detail = result.Message
                    });
                }
            }
            else
            {
                NotificationService.Notify(new Radzen.NotificationMessage
                {
                    Duration = 4_000,
                    CloseOnClick = true,
                    Severity = Radzen.NotificationSeverity.Error,
                    Summary = "Saving error",
                    Detail = "Command pattern or repository pattern not installed to perform the task. It's not possible to save the current item."
                });
            }
        }
        private async Task CheckIfYouWantToDeleteAsync()
        {
            _ = await DialogService.OpenAsync<Popup>("Delete confirmation",
                new Dictionary<string, object>
                {
                    { "Ok", () => DeleteAsync() },
                    { "Message", $"Are you sure to delete the current item with key {Key.FromBase64<TKey>()}" },
                });
        }
        private async ValueTask DeleteAsync()
        {
            if (Command != null)
            {
                LoadService.Show();
                var result = await Command.DeleteAsync(_entity.Key).NoContext();
                if (result.IsOk)
                    NavigationManager.NavigateTo($"../../../../Repository/{typeof(T).Name}/Query");
                else
                {
                    NotificationService.Notify(new Radzen.NotificationMessage
                    {
                        Duration = 4_000,
                        CloseOnClick = true,
                        Severity = Radzen.NotificationSeverity.Error,
                        Summary = "Deleting error",
                        Detail = "Command pattern or repository pattern not installed to perform the task. It's not possible to save the current item."
                    });
                    LoadService.Hide();
                }
            }
            else
            {
                NotificationService.Notify(new Radzen.NotificationMessage
                {
                    Duration = 4_000,
                    CloseOnClick = true,
                    Severity = Radzen.NotificationSeverity.Error,
                    Summary = "Deleting error",
                    Detail = "Command pattern or repository pattern not installed to perform the task. It's not possible to delete the current item."
                });
            }
        }
        private TKey? _keyBeforeEdit;
        private void ChangeKeyEditingStatus(bool x)
        {
            _isNew = x;
            if (_isNew)
                _keyBeforeEdit = _entity.Key;
            else
                _entity.Key = _keyBeforeEdit!;
        }
    }
}
