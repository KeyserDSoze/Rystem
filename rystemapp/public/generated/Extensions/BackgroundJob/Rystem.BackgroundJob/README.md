## Background job
Background job is a library that helps to create a background thread in your application (webapp or similar).
With the dependency injection pattern, you may set che CRON value (when your job has to run), [link to create CRON](https://crontab.guru/).
You may set to true the RunImmediately if you want to run one time during your bootstrap.
You may set the Key to allow the possibility to have more than one job with the same class on the same instance.

	builder.Services.AddBackgroundJob<BackgroundJob>(
        x =>
        {
            x.Cron = "*/1 * * * *";
            x.RunImmediately = true;
            x.Key = "alzo";
        });

Your class BackgroundJob has to extend IBackgrounJob and you may warm up it during the bootstrap.

    var app = builder.Build();
    await app.Services.WarmUpAsync();

In IBackgroundJob you have ActionToDoAsync, it's the main method, called when the cron is fired; and OnException to catch the possibile exception during the main method run.