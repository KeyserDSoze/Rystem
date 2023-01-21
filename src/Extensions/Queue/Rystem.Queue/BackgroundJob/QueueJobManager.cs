using Cronos;
using Microsoft.Extensions.DependencyInjection;
using System.Timers;

namespace Rystem.Queue
{
    internal sealed class QueueJobManager<T> : IBackgroundJob
    {
        private readonly IQueue<T> _queue;
        private readonly QueueProperty<T> _property;
        private readonly IServiceProvider _serviceProvider;
        private DateTime _nextFlush = DateTime.UtcNow;
        public QueueJobManager(IQueue<T> queue, QueueProperty<T> property, IServiceProvider serviceProvider)
        {
            _queue = queue;
            _property = property;
            _serviceProvider = serviceProvider;
        }
        public async Task ActionToDoAsync()
        {
            if (await _queue.CountAsync().NoContext() > _property.MaximumBuffer || _nextFlush < DateTime.UtcNow)
            {
                var expression = CronExpression.Parse(_property.MaximumRetentionCronFormat, _property.MaximumRetentionCronFormat?.Split(' ').Length > 5 ? CronFormat.IncludeSeconds : CronFormat.Standard);
                _nextFlush = expression.GetNextOccurrence(DateTime.UtcNow, true) ?? DateTime.UtcNow;
                List<T> items = new();
                foreach (var item in await _queue.DequeueAsync().NoContext())
                    items.Add(item);
                var service = _serviceProvider.CreateScope().ServiceProvider.GetService<IQueueManager<T>>();
                if (service != null)
                    await service.ManageAsync(items).NoContext();
            }
        }

        public Task OnException(Exception exception)
        {
            throw exception;
        }
    }
}