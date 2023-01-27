using Microsoft.JSInterop;

namespace Rystem.Web.Components.Services
{
    internal sealed class CopyService : ICopyService
    {
        private readonly IJSRuntime _jsInterop;
        public CopyService(IJSRuntime jsInterop)
            => _jsInterop = jsInterop;
        public async ValueTask CopyAsync(string value)
            => await _jsInterop.InvokeVoidAsync("navigator.clipboard.writeText", value).NoContext();
    }
}
