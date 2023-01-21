// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Localization;

internal sealed class MultipleOptions : IOptions<MultipleLocalizationOptions>
{
    public MultipleLocalizationOptions Value { get; }
    public MultipleOptions(MultipleLocalizationOptions value)
    {
        Value = value;
    }
}
