using Microsoft.JSInterop;
using Radzen;

namespace RepositoryFramework.Web.Components.Services
{
    internal sealed class CopyService : ICopyService
    {
        private readonly IJSRuntime _jsInterop;
        private readonly NotificationService _notificationService;
        public CopyService(IJSRuntime jsInterop, NotificationService notificationService)
        {
            _jsInterop = jsInterop;
            _notificationService = notificationService;
        }
        private const string Copied = nameof(Copied);
        private const string WithSuccess = "with success.";
        public async ValueTask CopyAsync(string value)
        {
            await _jsInterop.InvokeVoidAsync("navigator.clipboard.writeText", value).NoContext();
            _notificationService.Notify(new Radzen.NotificationMessage
            {
                Duration = 1_000,
                CloseOnClick = true,
                Severity = Radzen.NotificationSeverity.Success,
                Summary = Copied,
                Detail = WithSuccess
            });
        }
    }
}
