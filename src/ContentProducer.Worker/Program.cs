using ContentProducer.Worker;

bool runOnce = args.Any(arg => string.Equals(arg, "run-once", StringComparison.OrdinalIgnoreCase));
bool skipInstagram = args.Any(arg => string.Equals(arg, "--skip-instagram", StringComparison.OrdinalIgnoreCase));
bool skipTelegram = args.Any(arg => string.Equals(arg, "--skip-telegram", StringComparison.OrdinalIgnoreCase));
bool useFixture = args.Any(arg => string.Equals(arg, "--use-fixture", StringComparison.OrdinalIgnoreCase));
string[] hostArgs = args
    .Where(arg => !string.Equals(arg, "run-once", StringComparison.OrdinalIgnoreCase))
    .Where(arg => !string.Equals(arg, "--skip-instagram", StringComparison.OrdinalIgnoreCase))
    .Where(arg => !string.Equals(arg, "--skip-telegram", StringComparison.OrdinalIgnoreCase))
    .Where(arg => !string.Equals(arg, "--use-fixture", StringComparison.OrdinalIgnoreCase))
    .ToArray();

IHost host = Host.CreateDefaultBuilder(hostArgs)
    .ConfigureAppConfiguration(configuration =>
    {
        if (skipInstagram)
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{PublishingOptions.SectionName}:PublishToInstagram"] = "false"
            });
        }

        if (skipTelegram)
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{PublishingOptions.SectionName}:PublishToTelegram"] = "false"
            });
        }

        if (useFixture)
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{ContentGenerationOptions.SectionName}:LlmProvider"] =
                    ProviderNames.Fixture,
                [$"{ContentGenerationOptions.SectionName}:ImageProvider"] =
                    ProviderNames.Fixture
            });
        }
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<SchedulerOptions>(
            context.Configuration.GetSection(SchedulerOptions.SectionName));
        services.Configure<OpenAiOptions>(
            context.Configuration.GetSection(OpenAiOptions.SectionName));
        services.Configure<GroqOptions>(
            context.Configuration.GetSection(GroqOptions.SectionName));
        services.Configure<PollinationsOptions>(
            context.Configuration.GetSection(PollinationsOptions.SectionName));
        services.Configure<FixtureOptions>(
            context.Configuration.GetSection(FixtureOptions.SectionName));
        services.Configure<ContentGenerationOptions>(
            context.Configuration.GetSection(ContentGenerationOptions.SectionName));
        services.Configure<WordPressOptions>(
            context.Configuration.GetSection(WordPressOptions.SectionName));
        services.Configure<InstagramOptions>(
            context.Configuration.GetSection(InstagramOptions.SectionName));
        services.Configure<TelegramOptions>(
            context.Configuration.GetSection(TelegramOptions.SectionName));
        services.Configure<PublishingOptions>(
            context.Configuration.GetSection(PublishingOptions.SectionName));

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
        services.AddSingleton<GroqApiClient>();
        services.AddSingleton<ILlmProvider, OpenAiLlmProvider>();
        services.AddSingleton<ILlmProvider, GroqLlmProvider>();
        services.AddSingleton<ILlmProvider, FixtureLlmProvider>();
        services.AddSingleton<IImageProvider, OpenAiImageProvider>();
        services.AddSingleton<IImageProvider, PollinationsImageProvider>();
        services.AddSingleton<IImageProvider, FixtureImageProvider>();
        services.AddSingleton<IImageProvider, NoneImageProvider>();
        services.AddSingleton<IContentGeneratorService, ContentGeneratorService>();
        services.AddSingleton<IWordPressPublisherService, WordPressPublisherService>();
        services.AddSingleton<IInstagramPublisherService, InstagramPublisherService>();
        services.AddSingleton<ITelegramPublisherService, TelegramPublisherService>();
        services.AddSingleton<IContentProductionService, ContentProductionService>();

        if (!runOnce)
        {
            services.AddHostedService<SchedulerAgent>();
        }
    })
    .Build();

if (runOnce)
{
    await host.StartAsync();

    IContentProductionService contentProductionService =
        host.Services.GetRequiredService<IContentProductionService>();

    await contentProductionService.RunAsync(CancellationToken.None);
    await host.StopAsync();
}
else
{
    await host.RunAsync();
}
