using System.Collections.Generic;
using System.Linq;
using Xunit.Sdk;
using Xunit.v3;

namespace RepositoryFramework.UnitTest
{
    public class PriorityOrderer : ITestCollectionOrderer
    {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(
            IEnumerable<TTestCase> testCases)
            where TTestCase : ITestCase
        {
            var assemblyName = typeof(PriorityAttribute).AssemblyQualifiedName!;
            var sortedMethods = new SortedDictionary<int, List<TTestCase>>();
            foreach (TTestCase testCase in testCases)
            {
                //var priority = testCase.TestMethod.Method
                //    .GetCustomAttributes(assemblyName)
                //    .FirstOrDefault()
                //    ?.GetNamedArgument<int>(nameof(PriorityAttribute.Priority)) ?? 0;

                //GetOrCreate(sortedMethods, priority).Add(testCase);
                GetOrCreate(sortedMethods, 1).Add(testCase);
            }

            foreach (var testCase in
                sortedMethods.Keys.SelectMany(
                    priority => sortedMethods[priority].OrderBy(
                        testCase => testCase.TestMethod?.MethodName)))
            {
                yield return testCase;
            }
        }

        private static TValue GetOrCreate<TKey, TValue>(
            IDictionary<TKey, TValue> dictionary, TKey key)
            where TKey : struct
            where TValue : new() =>
            dictionary.TryGetValue(key, out var result)
                ? result
                : (dictionary[key] = new TValue());

        public IReadOnlyCollection<TTestCollection> OrderTestCollections<TTestCollection>(IReadOnlyCollection<TTestCollection> testCollections) where TTestCollection : ITestCollection
        {
            return [.. testCollections.OrderBy(x => x.UniqueID)];
        }
    }
}
