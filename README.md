# Content Producer

Content Producer is an automated content production project. The first MVP
contains a small scheduler agent that starts the content production service at
a configured time every day.

## Current MVP

- .NET 10 C# Worker Service
- One daily execution time
- Configurable time zone
- Reads an article prompt from a file on the host
- Generates a Persian article using the OpenAI Responses API
- Uses OpenAI web search for fresh news research
- Generates one website article image using the OpenAI Image API
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

Set the OpenAI API key as an environment variable. Do not commit the key:

```bash
export OPENAI_API_KEY="your-api-key"
```

OpenAI API billing is separate from a free ChatGPT account. A smaller model
reduces cost, but API requests still require an API project with available
credit or billing.

Configure OpenAI models and prompt paths in
`src/ContentProducer.Worker/appsettings.json`:

```json
{
  "OpenAI": {
    "ArticleModel": "gpt-5-mini",
    "ImageModel": "gpt-image-1-mini",
    "EnableWebSearch": false,
    "ImageSize": "1536x1024",
    "ImageQuality": "low",
    "PromptFilePath": "prompts/article-simple-fa.md",
    "ImagePromptFilePath": "prompts/article-image-fa.md"
  }
}
```

Choose the active prompt by changing `OpenAI:PromptFilePath`:

```json
"PromptFilePath": "prompts/article-simple-fa.md"
```

The simple prompt is intended for local testing and does not need web search.
For the full news prompt, use:

```json
"EnableWebSearch": true,
"PromptFilePath": "prompts/article-news-fa.md"
```

The prompt selection is stored directly in `appsettings.json`; no environment
variable is required for it.

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
export WORDPRESS_APPLICATION_PASSWORD="your-wordpress-application-password"
export TELEGRAM_BOT_TOKEN="your-telegram-bot-token"
```

You can also enable fixture loading in `appsettings.json`:

```json
"ContentSource": {
  "UseFixture": true,
  "FixtureArticleFilePath": "fixtures/article-example.json",
  "FixtureImageFilePath": "fixtures/images/article-image.png"
}
```

Set `UseFixture` back to `false` to use OpenAI again.

Set WordPress, Instagram, and Telegram secrets as environment variables:

```bash
export WORDPRESS_APPLICATION_PASSWORD="your-wordpress-application-password"
export INSTAGRAM_ACCESS_TOKEN="your-instagram-access-token"
export TELEGRAM_BOT_TOKEN="your-telegram-bot-token"
```

Then configure the WordPress site, username, Instagram User ID, Telegram channel
chat ID, and Graph API version in `appsettings.json`.

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

To test only OpenAI and WordPress while leaving Instagram for later:

```bash
dotnet run --project src/ContentProducer.Worker -- run-once --skip-instagram --skip-telegram
```

For this mode, set only these secrets:

```bash
export OPENAI_API_KEY="your-api-key"
export WORDPRESS_APPLICATION_PASSWORD="your-wordpress-application-password"
```

On Windows PowerShell:

```powershell
$env:OPENAI_API_KEY="your-api-key"
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
setup, manual testing, logs, and update instructions.
