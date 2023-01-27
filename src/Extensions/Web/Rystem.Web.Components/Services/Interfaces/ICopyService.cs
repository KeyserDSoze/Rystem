using Microsoft.AspNetCore.Components;

namespace Rystem.Web.Components.Services
{

    public interface ICopyService
    {
        ValueTask CopyAsync(string value);
    }
}
