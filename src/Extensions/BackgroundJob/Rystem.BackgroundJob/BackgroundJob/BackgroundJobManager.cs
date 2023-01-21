using Cronos;
using System.Collections.Concurrent;
using System.Threading.Concurrent;

namespace System.Timers
{
    internal sealed class BackgroundJobManager : IBackgroundJobManager
    {
        private readonly ConcurrentDictionary<string, Timer> Actions = new();
        private readonly ILock _lockService;

        public BackgroundJobManager(ILock lockService)
        {
            _lockService = lockService;
        }
        public Task RunAsync(IBackgroundJob job,
            BackgroundJobOptions options,
            Func<IBackgroundJob>? factory = null,
            CancellationToken cancellationToken = default)
        {
            string key = $"BackgroundWork_{options.Key}_{job.GetType().FullName}";
            return _lockService
                .ExecuteAsync(async () =>
                {
                    if (Actions.ContainsKey(key))
                    {
                        Actions[key].Stop();
                        Actions.TryRemove(key, out _);
                    }
                    if (options.RunImmediately)
                    {
                        try
                        {
                            await job.ActionToDoAsync().NoContext();
                        }
                        catch (Exception ex)
                        {
                            await job.OnException(ex).NoContext();
                        }
                    }
                    NewTimer();

                    void NewTimer()
                    {
                        var expression = CronExpression.Parse(options.Cron, options.Cron?.Split(' ').Length > 5 ? CronFormat.IncludeSeconds : CronFormat.Standard);
                        var nextRunningTime = expression.GetNextOccurrence(DateTime.UtcNow, true)?.Subtract(DateTime.UtcNow).TotalMilliseconds ?? 120;
                        bool runAction = true;
                        if (nextRunningTime > int.MaxValue)
                        {
                            runAction = false;
                            nextRunningTime = int.MaxValue;
                        }
                        var nextTimeTimer = new Timer
                        {
                            Interval = nextRunningTime
                        };
                        nextTimeTimer.Elapsed += async (x, e) =>
                        {
                            job = factory?.Invoke() ?? job;
                            nextTimeTimer.Stop();
                            Actions.TryRemove(key, out _);
                            if (!(cancellationToken != default && cancellationToken.IsCancellationRequested))
                            {
                                if (runAction)
                                {
                                    try
                                    {
                                        await job.ActionToDoAsync().NoContext();
                                    }
                                    catch (Exception ex)
                                    {
                                        await job.OnException(ex).NoContext();
                                    }
                                }
                                NewTimer();
                            }
                        };
                        nextTimeTimer.Start();
                        Actions.TryAdd(key, nextTimeTimer);
                    }
                }, $"{nameof(BackgroundJobOptions)}{key}");
        }
    }
}