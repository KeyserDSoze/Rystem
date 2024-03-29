﻿using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.InMemory
{
    /// <summary>
    /// You may set the milliseconds (in range) for each request to simulate a real external database or storage.
    /// You may set a list of exceptions with a random percentage of throwing.
    /// </summary>
    /// <typeparam name="T">Model used for your repository.</typeparam>
    /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed", Justification = "It's not used but it's needed for the return methods that use this class.")]
    public sealed class RepositoryBehaviorSettings<T, TKey> : IFactoryOptions
        where TKey : notnull
    {
        private readonly Dictionary<RepositoryMethods, MethodBehaviorSetting> _settings = new();
        private void Add(RepositoryMethods method, MethodBehaviorSetting methodSettings)
        {
            if (!_settings.ContainsKey(method))
                _settings.Add(method, methodSettings);
            else
                _settings[method] = methodSettings;
        }
        public void AddForRepositoryPattern(MethodBehaviorSetting setting)
            => Add(RepositoryMethods.All, setting);
        public void AddForCommandPattern(MethodBehaviorSetting setting)
        {
            Add(RepositoryMethods.Insert, setting);
            Add(RepositoryMethods.Update, setting);
            Add(RepositoryMethods.Delete, setting);
            Add(RepositoryMethods.Batch, setting);
        }
        public void AddForQueryPattern(MethodBehaviorSetting setting)
        {
            Add(RepositoryMethods.Get, setting);
            Add(RepositoryMethods.Query, setting);
            Add(RepositoryMethods.Exist, setting);
            Add(RepositoryMethods.Operation, setting);
        }
        public void AddForInsert(MethodBehaviorSetting setting)
            => Add(RepositoryMethods.Insert, setting);
        public void AddForUpdate(MethodBehaviorSetting setting)
            => Add(RepositoryMethods.Update, setting);
        public void AddForDelete(MethodBehaviorSetting setting)
            => Add(RepositoryMethods.Delete, setting);
        public void AddForBatch(MethodBehaviorSetting setting)
            => Add(RepositoryMethods.Batch, setting);
        public void AddForGet(MethodBehaviorSetting setting)
            => Add(RepositoryMethods.Get, setting);
        public void AddForQuery(MethodBehaviorSetting setting)
            => Add(RepositoryMethods.Query, setting);
        public void AddForExist(MethodBehaviorSetting setting)
            => Add(RepositoryMethods.Exist, setting);
        public void AddForCount(MethodBehaviorSetting setting)
            => Add(RepositoryMethods.Operation, setting);
        public MethodBehaviorSetting Get(RepositoryMethods method)
        {
            if (_settings.TryGetValue(method, out var value))
                return value;
            else if (_settings.TryGetValue(RepositoryMethods.All, out var allValue))
                return allValue;
            else
                return MethodBehaviorSetting.Default;
        }
    }
}
