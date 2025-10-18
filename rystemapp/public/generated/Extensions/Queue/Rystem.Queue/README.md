## Queue
You have to configure it in DI
BackgroundJobCronFormat is the CRON for background job that checks if Maximum buffer is exceeded or has a retention expired, usually is lesser than or equal of MaximumRetentionCronFormat.
MaximumRetentionCronFormat is the CRON for maximum time before to empty the queue and call the IQueueManager<T>.
MaximumBuffer is the maximum queue length before to empty the queue and call the IQueueManager<T>.

	services.AddMemoryQueue<Sample, SampleQueueManager>(x =>
        {
            x.MaximumBuffer = 1000;
            x.MaximumRetentionCronFormat = "*/3 * * * * *";
            x.BackgroundJobCronFormat = "*/1 * * * * *";
        });

    public class SampleQueueManager : IQueueManager<Sample> 
    {
        public Task ManageAsync(IEnumerable<Sample> items)
        {
            return Task.CompletedTask;
        }
    }

For instance, in the example above you have a maximum queue length of 1000, a background job thatc checks every 1 second if there are 1000 or more items or maximum retention period of 3 seconds is expired.
after the build you have to warm up

    var app = builder.Build();
	await app.Services.WarmUpAsync();

and inject to use it
    
    var queue = _serviceProvider.GetService<IQueue<Sample>>()!;
    for (int i = 0; i < 100; i++)
        await queue.AddAsync(new Sample() { Id = i.ToString() });

In this example, after 1000 elements or 3 seconds the configured actions will be fired and the queue will be emptied.

### Stack (Last In First Out)

    services.AddMemoryStackQueue<Sample, SampleQueueManager>(x =>
        {
            x.MaximumBuffer = 1000;
            x.MaximumRetentionCronFormat = "*/3 * * * * *";
            x.BackgroundJobCronFormat = "*/1 * * * * *";
        });

### Custom integration
If you want to use a distributed queue like storage queue, or event hub or service bus or event grid, you can write your own integration and configure it.

    services.AddQueueIntegration<Sample, SampleQueueManager, YourQueueIntegration>(x =>
        {
            x.MaximumBuffer = 1000;
            x.MaximumRetentionCronFormat = "*/3 * * * * *";
            x.BackgroundJobCronFormat = "*/1 * * * * *";
        });