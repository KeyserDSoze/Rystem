using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Rystem.Web.Components.Customization;

namespace Rystem.Web.Components.Contents
{
    public partial class Table<T> : ITable
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        public string X { get; set; }
    }
}
