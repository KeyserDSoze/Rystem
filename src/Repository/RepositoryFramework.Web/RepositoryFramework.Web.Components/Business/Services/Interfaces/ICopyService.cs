using Microsoft.AspNetCore.Components;

namespace RepositoryFramework.Web.Components.Services
{

    public interface ICopyService
    {
        ValueTask CopyAsync(string value);
    }
}
