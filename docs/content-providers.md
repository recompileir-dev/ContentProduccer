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
- `Fixture`: loads the committed example image without an API request

Groq is currently used for text content only. To use Groq articles with
generated images, select `Groq` as the LLM provider and `OpenAI` as the image
provider.

## Provider Selection

```json
{
  "ContentGeneration": {
    "LlmProvider": "Groq",
    "ImageProvider": "OpenAI",
    "PromptFilePath": "prompts/article-news-fa.md",
    "ImagePromptFilePath": "prompts/article-image-fa.md"
  }
}
```

Provider names are case-insensitive. Unknown provider names fail at runtime and
the error lists the available providers.

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
    "Model": "groq/compound",
    "MaxCompletionTokens": 8192
  }
}
```

Set the key:

```bash
export GROQ_API_KEY="your-groq-api-key"
```

The default `groq/compound` system is suitable for the news prompt because it
can use external tools such as web search. Groq uses JSON Object Mode and the
same article prompt and output contract as OpenAI.

## Code Structure

```text
Application/              workflow orchestration and formatting
Configuration/            provider and workflow options
Domain/                   generated content models and parsing
Providers/Abstractions/   LLM and image provider contracts
Providers/OpenAI/         OpenAI article and image providers
Providers/Groq/           Groq article provider
Providers/Fixture/        fake providers backed by fixture files
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

- `prompts/article-simple-fa.md`: simple article prompt for local testing
- `prompts/article-news-fa.md`: news prompt that examines two or three fresh AI-life-impact news items
- `prompts/article-image-fa.md`: editable website image prompt

Article and image prompt paths are shared across providers and can be edited
without changing provider code.

## Structured Article Output

Every LLM provider must return valid JSON containing:

- `title`
- `articleHtml`
- `instagramCaption`
- `focusKeyphrase`
- `seoTitle`
- `metaDescription`
- `references`
