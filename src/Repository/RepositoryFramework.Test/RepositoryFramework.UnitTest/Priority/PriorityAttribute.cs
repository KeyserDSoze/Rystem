using System;

namespace RepositoryFramework.UnitTest
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PriorityAttribute : Attribute
    {
        public int Priority { get; }
        public PriorityAttribute(int priority) => Priority = priority;
    }
}
