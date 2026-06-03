using ContentProducer.Worker;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<SchedulerOptions>(
            context.Configuration.GetSection(SchedulerOptions.SectionName));
        services.Configure<OpenAiOptions>(
            context.Configuration.GetSection(OpenAiOptions.SectionName));

        services.AddSingleton(sp =>
        {
            OpenAiOptions options = sp.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<OpenAiOptions>>().Value;

            return new HttpClient
            {
                BaseAddress = new Uri(options.BaseUrl),
                Timeout = TimeSpan.FromMinutes(10)
            };
        });
        services.AddSingleton<OpenAiApiClient>();
        services.AddSingleton<IOpenAiArticleService, OpenAiArticleService>();
        services.AddSingleton<IOpenAiImageService, OpenAiImageService>();
        services.AddSingleton<IContentProductionService, ContentProductionService>();
        services.AddHostedService<SchedulerAgent>();
    })
    .Build();

await host.RunAsync();
