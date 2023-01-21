// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Localization;

public interface IMultipleStringLocalizerFactory
{
    IStringLocalizerFactory GetFactory(Type type);
}
