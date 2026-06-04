# Content Producer

Content Producer is an automated content production project. The first MVP
contains a small scheduler agent that starts the content production service at
a configured time every day.

## Current MVP

- .NET 10 C# Worker Service
- One daily execution time
- Configurable time zone
- Reads an article prompt from a file on the host
- Generates a Persian article through a configurable LLM provider
- Supports OpenAI, Groq, and fixture LLM providers
- Optionally generates one website article image using the OpenAI Image API
- Uploads the generated image directly to the WordPress Media Library
- Publishes the article to WordPress with the image as featured media
- Publishes the generated image and summary to Instagram
- Publishes the generated image and summary to a Telegram channel
- Uses the generated Instagram caption without saving article or image files locally

This version does not use a database, distributed lock, retry policy, or
external scheduler package.

## Run

Set the schedule in
`src/ContentProducer.Worker/appsettings.json`:

```json
{
  "Scheduler": {
    "StartAt": "08:00:00",
    "TimeZoneId": "Asia/Tehran"
  }
}
```

Set API keys for the providers you use as environment variables. Do not commit
keys:

```bash
export OPENAI_API_KEY="your-api-key"
export GROQ_API_KEY="your-groq-api-key"
```

OpenAI API billing is separate from a free ChatGPT account. A smaller model
reduces cost, but API requests still require an API project with available
credit or billing. `OPENAI_API_KEY` is still required when OpenAI is selected
as the image provider, even if Groq generates the article.

Configure the active providers and prompt paths in
`src/ContentProducer.Worker/appsettings.json`:

```json
{
  "ContentGeneration": {
    "LlmProvider": "OpenAI",
    "ImageProvider": "OpenAI",
    "PromptFilePath": "prompts/article-news-fa.md",
    "ProviderPromptFilePaths": {
      "Groq": "prompts/article-news-groq-fa.md"
    },
    "ImagePromptFilePath": "prompts/article-image-fa.md"
  },
  "OpenAI": {
    "ArticleModel": "gpt-5-mini",
    "ImageModel": "gpt-image-1-mini",
    "EnableWebSearch": false,
    "ImageSize": "1536x1024",
    "ImageQuality": "low"
  },
  "Groq": {
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
    "ResearchPromptMaxCharacters": 1200,
    "ContinueWithoutResearchOnFailure": true
  }
}
```

Choose the LLM provider by changing `ContentGeneration:LlmProvider`:

```json
"LlmProvider": "Groq"
```

Valid LLM providers are `OpenAI`, `Groq`, and `Fixture`. The image provider can
be `OpenAI`, `Fixture`, or `None`. Choose `None` when an article should be
published without calling an image API. Choose the active article prompt with:

```json
"PromptFilePath": "prompts/article-news-fa.md"
```

`PromptFilePath` is the default prompt. A provider can override it through
`ProviderPromptFilePaths`; the included Groq override uses a more explicit
prompt for article depth, structure, and minimum length.

When using OpenAI for fresh news research, also set `OpenAI:EnableWebSearch` to
`true`. Groq performs web research with `groq/compound-mini`, then uses its
writer model to produce the structured article. If Compound returns HTTP `413`
during web research, the default configuration logs a warning and lets the
writer continue without research notes.

The image prompt is also editable without changing code. It asks the model to
compose a horizontal article image suitable for display at 790 pixels wide.
The API request uses the supported landscape output size `1536x1024`.

## Test With Example Content

The project includes an example article and example image under
`src/ContentProducer.Worker/fixtures`. To publish this example without calling
OpenAI, run:

```bash
dotnet run --project src/ContentProducer.Worker -- run-once --use-fixture --skip-instagram
```

This tests WordPress and Telegram publishing. It requires only:

```bash
export WORDPRESS_USERNAME="your-wordpress-username"
export WORDPRESS_APPLICATION_PASSWORD="your-wordpress-application-password"
export TELEGRAM_BOT_TOKEN="your-telegram-bot-token"
```

You can also select fixture providers in `appsettings.json`:

```json
"ContentGeneration": {
  "LlmProvider": "Fixture",
  "ImageProvider": "Fixture"
},
"Fixture": {
  "ArticleFilePath": "fixtures/article-example.json",
  "ImageFilePath": "fixtures/images/article-image.png"
}
```

Set the providers back to `OpenAI` or `Groq` to use a real LLM again.

Groq does not provide an image generation endpoint. To generate an article with
Groq without using OpenAI billing, configure:

```json
"ContentGeneration": {
  "LlmProvider": "Groq",
  "ImageProvider": "None"
}
```

In this mode WordPress receives a text-only post, Telegram receives a text
message, and Instagram publishing is skipped because Instagram feed posts
require media.

Set publishing secrets as environment variables:

```bash
export WORDPRESS_USERNAME="your-wordpress-username"
export WORDPRESS_APPLICATION_PASSWORD="your-wordpress-application-password"
export INSTAGRAM_ACCESS_TOKEN="your-instagram-access-token"
export TELEGRAM_BOT_TOKEN="your-telegram-bot-token"
```

Then configure the WordPress site, Instagram User ID, Telegram channel chat ID,
and Graph API version in `appsettings.json`.

Then run:

```bash
dotnet run --project src/ContentProducer.Worker
```

Each run sends generated content directly to WordPress, Instagram, and
Telegram. The WordPress Media Library provides the public image URL required by
the social publishing APIs.

## Local Test Without Scheduler

To run the workflow once from the command line instead of waiting for the
scheduler:

```bash
dotnet run --project src/ContentProducer.Worker -- run-once
```

To test only the selected content providers and WordPress while leaving social
publishing for later:

```bash
dotnet run --project src/ContentProducer.Worker -- run-once --skip-instagram --skip-telegram
```

For this mode, set only these secrets:

```bash
export WORDPRESS_USERNAME="your-wordpress-username"
export OPENAI_API_KEY="your-api-key"
export WORDPRESS_APPLICATION_PASSWORD="your-wordpress-application-password"
```

On Windows PowerShell:

```powershell
$env:OPENAI_API_KEY="your-api-key"
$env:WORDPRESS_USERNAME="your-wordpress-username"
$env:WORDPRESS_APPLICATION_PASSWORD="your-wordpress-application-password"
dotnet run --project src/ContentProducer.Worker -- run-once --skip-instagram --skip-telegram
```

More project documentation is available in [docs](./docs/README.md).

## Docker Deployment

The project includes a production Dockerfile and Docker Compose configuration
for a dedicated Linux server:

```bash
cp .env.example .env
docker compose up -d --build
docker compose logs -f content-producer
```

The `.env` file contains deployment secrets and is ignored by Git. See the
[Docker deployment guide](./docs/docker-deployment.md) for complete server
setup, manual testing, logs, and update instructions. A Persian step-by-step
checklist is available in the
[server deployment guide](./docs/server-deployment-step-by-step-fa.md).
