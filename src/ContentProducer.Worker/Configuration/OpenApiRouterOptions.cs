namespace ContentProducer.Worker;

public sealed class OpenApiRouterOptions
{
    public const string SectionName = "OpenApiRouter";

    public string? ApiKey { get; init; }

    public string ApiKeyEnvironmentVariable { get; init; } = "OPENAPI_ROUTER_API_KEY";

    public string BaseUrl { get; init; } = "https://api.openapirouter.example/v1/";

    public string ArticleModel { get; init; } = "router-model";
    
    public string ImageModel { get; init; } = "router-image-1";

    public string ImageSize { get; init; } = "1536x1024";

    public string ImageQuality { get; init; } = "low";
}
