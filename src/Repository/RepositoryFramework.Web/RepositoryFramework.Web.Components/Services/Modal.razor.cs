using global::System;
using global::System.Collections.Generic;
using global::System.Linq;
using global::System.Threading.Tasks;
using global::Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor;
using Radzen;
using RepositoryFramework.Web.Components.Business.Language;
using RepositoryFramework.Web.Components.Services;
using RepositoryFramework.Web.Components.Extensions;
using System.Reflection;

namespace RepositoryFramework.Web.Components.Standard
{
    public partial class Modal
    {
        [Parameter]
        public required Action Ok { get; set; }

        [Parameter]
        public required string Title { get; set; }

        [Parameter]
        public required string Message { get; set; }

        [Parameter]
        public required Action Cancel { get; set; }
    }
}
