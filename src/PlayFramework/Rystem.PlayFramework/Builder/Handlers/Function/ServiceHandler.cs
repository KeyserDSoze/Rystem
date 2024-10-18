﻿namespace Rystem.PlayFramework
{
    internal sealed class ServiceHandler
    {
        public Dictionary<string, Func<Dictionary<string, string>, ServiceBringer, ValueTask>> Actions { get; } = [];
        public required Func<IServiceProvider, ServiceBringer, Task<object?>> Call { get; set; }
    }
}