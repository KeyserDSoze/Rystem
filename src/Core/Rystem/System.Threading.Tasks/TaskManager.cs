using System.Collections.Concurrent;

namespace System.Threading.Tasks
{
    public static class TaskManager
    {
        public static Task WhenAll(Func<int, CancellationToken, Task> task, int times, int concurrentTask = 8, bool runEverytimeASlotIsFree = false, CancellationToken cancellationToken = default)
        {
            var ints = new List<int>();
            for (var i = 0; i < times; i++)
                ints.Add(i);
            return WhenAll(task, ints, concurrentTask, runEverytimeASlotIsFree, cancellationToken);
        }
        public static async Task WhenAll<T>(Func<T, CancellationToken, Task> task, IList<T> inputs, int concurrentTask = 8, bool runEverytimeASlotIsFree = false, CancellationToken cancellationToken = default)
        {
            var tasks = new ConcurrentList<Task>();
            for (var i = 0; i < inputs.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                tasks.Add(task.Invoke(inputs[i], cancellationToken));
                while (tasks.Count >= concurrentTask)
                {
                    if (runEverytimeASlotIsFree)
                    {
                        for (var j = 0; j < tasks.Count; j++)
                        {
                            if (tasks[j].IsCompleted)
                            {
                                tasks.RemoveAt(j);
                                j--;
                            }
                        }
                        await Task.Delay(10, cancellationToken).NoContext();
                    }
                    else
                    {
                        await Task.WhenAll(tasks).NoContext();
                        tasks.Clear();
                    }
                }
            }
            if (tasks.Count > 0)
                await Task.WhenAll(tasks).NoContext();
        }
        public static Task WhenAtLeast(Func<int, CancellationToken, Task> task, int times, int atLeast, int concurrentTask = 8, CancellationToken cancellationToken = default)
        {
            var ints = new List<int>();
            for (var i = 0; i < times; i++)
                ints.Add(i);
            return WhenAtLeast(task, ints, atLeast, concurrentTask, cancellationToken);
        }
        public static async Task WhenAtLeast<T>(Func<T, CancellationToken, Task> task, IList<T> inputs, int atLeast, int concurrentTask = 8, CancellationToken cancellationToken = default)
        {
            var tasks = new ConcurrentList<Task>();
            var completed = 0;
            var runTillTheEnd = false;
            for (var i = 0; i < inputs.Count; i++)
            {
                tasks.Add(task.Invoke(inputs[i], cancellationToken));
                if (!runTillTheEnd)
                {
                    while (tasks.Count >= concurrentTask)
                    {
                        await Task.Delay(10, cancellationToken).NoContext();
                        for (var j = 0; j < tasks.Count; j++)
                        {
                            if (tasks[j].IsCompleted)
                            {
                                tasks.RemoveAt(j);
                                j--;
                                completed++;
                                runTillTheEnd = completed >= atLeast;
                            }
                        }
                    }
                }
            }
            if (tasks.Count > 0)
            {
                await Task.Delay(10, cancellationToken).NoContext();
                for (var j = 0; j < tasks.Count; j++)
                {
                    if (tasks[j].IsCompleted)
                    {
                        tasks.RemoveAt(j);
                        j--;
                        completed++;
                        if (completed >= atLeast)
                            return;
                    }
                }
            }
        }
    }
}
