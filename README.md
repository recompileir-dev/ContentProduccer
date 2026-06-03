# Content Producer

Content Producer is an automated content production project. The first MVP
contains a small scheduler agent that starts the content production service at
a configured time every day.

## Current MVP

- C# Worker Service
- One daily execution time
- Configurable time zone
- Reads an article prompt from a file on the host
- Generates a Persian article using the OpenAI Responses API
- Uses OpenAI web search for fresh news research
- Generates Instagram carousel images using the OpenAI Image API
- Saves the article and images to a local output directory

This version does not use a database, distributed lock, retry policy, or
external scheduler package. It also does not publish to Instagram yet.

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

Configure OpenAI models, prompt path, image count, and output path in
`src/ContentProducer.Worker/appsettings.json`:

```json
{
  "OpenAI": {
    "ArticleModel": "gpt-5.5",
    "ImageModel": "gpt-image-2",
    "EnableWebSearch": true,
    "ImageCount": 4,
    "PromptFilePath": "prompts/article-news-fa.md",
    "OutputDirectory": "output"
  }
}
```

Then run:

```bash
dotnet run --project src/ContentProducer.Worker
```

Each run creates a timestamped directory containing `article.md` and carousel
PNG files.

More project documentation is available in [docs](./docs/README.md).
