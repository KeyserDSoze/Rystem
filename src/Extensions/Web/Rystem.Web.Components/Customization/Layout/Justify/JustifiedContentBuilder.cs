﻿using System.Text;

namespace Rystem.Web.Components.Customization
{
    public sealed class JustifiedContentBuilder<T> : BreakPointClassBuilder<DirectionClassBuilder<T>>
        where T : ICssClassBuilder
    {
        internal JustifiedContentBuilder(StringBuilder stringBuilder) : base(stringBuilder)
        {
            stringBuilder.Append(" justify-content");
        }
    }
}
