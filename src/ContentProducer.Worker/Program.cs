using ContentProducer.Worker;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<SchedulerOptions>(
            context.Configuration.GetSection(SchedulerOptions.SectionName));

        services.AddSingleton<IContentProductionService, ContentProductionService>();
        services.AddHostedService<SchedulerAgent>();
    })
    .Build();

await host.RunAsync();
