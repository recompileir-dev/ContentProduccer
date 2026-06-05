# Content Providers

The content generation workflow is provider-independent. `ContentGeneratorService`
depends on `ILlmProvider` for article generation and `IImageProvider` for image
generation.

## Available Providers

LLM providers:

- `OpenAI`: generates articles with the OpenAI Responses API
- `Groq`: generates articles with the Groq Chat Completions API
- `Fixture`: loads the committed example article without an API request

Image providers:

- `OpenAI`: generates one website article image
- `GoogleImages`: searches Google Images through the official Programmable Search JSON API and downloads an existing image
- `Pollinations`: generates one website article image from a free public endpoint without an API key
- `Fixture`: loads the committed example image without an API request
- `None`: disables image generation

Groq is used for text content only. Its API supports image understanding as
input, but it does not provide image generation. To use Groq articles with
found or generated images, select `Groq` as the LLM provider and `GoogleImages`,
`Pollinations`, or `OpenAI` as the image provider. To avoid using an image API,
select `None`.

## Provider Selection

```json
{
  "ContentGeneration": {
    "LlmProvider": "Groq",
    "ImageProvider": "GoogleImages",
    "PromptFilePath": "prompts/article-news-fa.md",
    "ProviderPromptFilePaths": {
      "Groq": "prompts/article-news-groq-fa.md"
    },
    "ImagePromptFilePath": "prompts/article-image-fa.md"
  }
}
```

Provider names are case-insensitive. Unknown provider names fail at runtime and
the error lists the available providers.

`PromptFilePath` is the fallback prompt for every LLM provider.
`ProviderPromptFilePaths` can override it by provider name. This allows OpenAI
and Groq to receive prompts tuned for their different APIs and model behavior
without changing provider code.

With `ImageProvider: None`, WordPress publishes without featured media,
Telegram publishes a text message, and Instagram is skipped because its feed
publishing API requires media.

## Google Images Settings

```json
{
  "GoogleImageSearch": {
    "ApiKeyEnvironmentVariable": "GOOGLE_IMAGE_SEARCH_API_KEY",
    "SearchEngineIdEnvironmentVariable": "GOOGLE_IMAGE_SEARCH_ENGINE_ID",
    "BaseUrl": "https://www.googleapis.com/customsearch/v1",
    "QueryTemplate": "{focusKeyphrase} {title} technology article",
    "SearchCount": 10,
    "MaxDownloadAttempts": 5,
    "MaxQueryCharacters": 180,
    "ImageSize": "large",
    "Safe": "active",
    "Rights": "",
    "MaxImageBytes": 8000000
  }
}
```

This provider does not create a new image. It searches Google Images through
the official Custom Search JSON API with `searchType=image`, downloads the first
usable image result, uploads it to WordPress, and stores the source page in the
image caption. It requires a Google API key and a Programmable Search Engine ID.

For safer reuse, configure the Programmable Search Engine and `Rights` filter
according to the licensing rules you want to follow. The provider records
attribution, but it does not automatically verify legal permission for every
image.

## Pollinations Settings

```json
{
  "Pollinations": {
    "BaseUrl": "https://image.pollinations.ai/prompt/",
    "Model": "flux",
    "Width": 1536,
    "Height": 1024,
    "NoLogo": true,
    "Private": true,
    "Safe": true,
    "Enhance": false,
    "MaxPromptCharacters": 700,
    "MaxAttempts": 3,
    "RetryDelaySeconds": 30
  }
}
```

Pollinations is useful when an article image is needed without OpenAI billing.
The default endpoint does not require an API key. Because it is a free public
service, availability, queue time, and output quality are not guaranteed like a
paid production API.

The provider uses a short editable prompt file, trims the prompt for URL
length, requests a landscape image, and sends the returned image content type to
WordPress. If the free queue is temporarily full, it retries a small number of
times before failing.

## OpenAI Settings

```json
{
  "OpenAI": {
    "ApiKeyEnvironmentVariable": "OPENAI_API_KEY",
    "BaseUrl": "https://api.openai.com/v1/",
    "ArticleModel": "gpt-5-mini",
    "ImageModel": "gpt-image-1-mini",
    "EnableWebSearch": true,
    "ImageSize": "1536x1024",
    "ImageQuality": "low"
  }
}
```

Set the key:

```bash
export OPENAI_API_KEY="your-api-key"
```

## Groq Settings

```json
{
  "Groq": {
    "ApiKeyEnvironmentVariable": "GROQ_API_KEY",
    "BaseUrl": "https://api.groq.com/openai/v1/",
    "WriterModel": "openai/gpt-oss-120b",
    "MaxCompletionTokens": 5000,
    "EnableWriterFallback": true,
    "FallbackWriterModel": "llama-3.3-70b-versatile",
    "FallbackMaxCompletionTokens": 4500,
    "MinimumArticleHtmlCharacters": 4500,
    "RejectShortArticles": true,
    "EnableWebResearch": true,
    "ResearchModel": "groq/compound-mini",
    "ResearchModelVersion": "2025-07-23",
    "ResearchMaxCompletionTokens": 2048,
    "ResearchPromptMaxCharacters": 1200,
    "MaxResearchCharacters": 12000,
    "ContinueWithoutResearchOnFailure": true
  }
}
```

Set the key:

```bash
export GROQ_API_KEY="your-groq-api-key"
```

The Groq provider uses two stages by default:

1. `groq/compound-mini` performs a focused web research request.
2. `openai/gpt-oss-120b` receives the original prompt plus the research
   notes and writes the structured JSON article.

Despite its model ID, `openai/gpt-oss-120b` is served through the Groq API and
uses the Groq API key and Groq billing. It is the default writer because it
followed the long-form Persian article instructions more reliably in testing.
The primary writer uses Groq Structured Outputs with a strict JSON Schema. If
Groq reports `json_validate_failed` or a failed JSON generation, the provider
retries once with `FallbackWriterModel` using JSON Object Mode.

This avoids asking a Compound system to perform web research and generate a
long JSON article in one request, which can result in HTTP `413 Request Entity
Too Large` during internal tool execution.

The research request receives only the first `ResearchPromptMaxCharacters`
characters of the article prompt. This keeps output-format and SEO instructions
out of the Compound web-search request. `ResearchModelVersion: 2025-07-23`
selects Compound Basic Search, which is a better fit for this small research
step than the more comprehensive Advanced Search used by newer versions. If
Compound still returns HTTP `413`,
`ContinueWithoutResearchOnFailure: true` logs a warning and lets the writer
model continue without research notes instead of stopping the Worker.

Disable web research with `EnableWebResearch: false` for prompts that do not
need fresh information. Increase token limits only when output is truncated.
The provider rejects an article when `articleHtml` is shorter than
`MinimumArticleHtmlCharacters` and `RejectShortArticles` is `true`. Set it to
`false` only when short articles are acceptable.

## Code Structure

```text
Application/              workflow orchestration and formatting
Configuration/            provider and workflow options
Domain/                   generated content models and parsing
Providers/Abstractions/   LLM and image provider contracts
Providers/OpenAI/         OpenAI article and image providers
Providers/Groq/           Groq article provider
Providers/GoogleImages/   Google image search provider
Providers/Pollinations/   free public image provider
Providers/Fixture/        fake providers backed by fixture files
Providers/None/           image generation opt-out provider
Publishing/               WordPress, Instagram, and Telegram integrations
Scheduling/               daily scheduler agent
```

## Fixture Settings

```json
{
  "Fixture": {
    "ArticleFilePath": "fixtures/article-example.json",
    "ImageFilePath": "fixtures/images/article-image.png"
  }
}
```

Select both fixture providers to test publishing without an LLM or image API:

```json
{
  "ContentGeneration": {
    "LlmProvider": "Fixture",
    "ImageProvider": "Fixture"
  }
}
```

The command-line shortcut remains available:

```bash
dotnet run --project src/ContentProducer.Worker -- run-once --use-fixture
```

## Prompt Files

- `prompts/article-news-fa.md`: news prompt that examines two or three fresh AI-life-impact news items
- `prompts/article-news-groq-fa.md`: Groq-specific news prompt with explicit depth and length requirements
- `prompts/article-image-fa.md`: editable website image prompt used by OpenAI
- `prompts/article-image-pollinations-en.md`: shorter editable image prompt used by Pollinations

Article prompt paths can be shared or overridden per provider. Image prompts
can also be edited without changing provider code.

## Structured Article Output

Every LLM provider must return valid JSON containing:

- `title`
- `articleHtml`
- `instagramCaption`
- `focusKeyphrase`
- `seoTitle`
- `metaDescription`
- `references`
