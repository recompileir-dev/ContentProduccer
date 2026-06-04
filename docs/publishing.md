# Automatic Publishing

## Flow

1. OpenAI generates the article, Instagram-style summary, and one article image.
2. The image is kept in memory and is not written to the Worker disk.
3. The image is uploaded directly to the WordPress Media Library.
4. The WordPress post is published with that image as featured media and as an image inside the post content.
5. The WordPress post link is read from the WordPress REST API response.
6. Instagram receives the generated image as a single-image post.
7. Instagram caption contains the summary and the original WordPress post URL.
8. Telegram receives the generated image, summary, and original WordPress post link.

The Worker does not save article or image files locally. WordPress Media Library
contains the single image used by the WordPress, Instagram, and Telegram posts.

## Manual Test Without Scheduler

Run the full workflow once:

```bash
dotnet run --project src/ContentProducer.Worker -- run-once
```

Run only OpenAI and WordPress, skipping Instagram and Telegram:

```bash
dotnet run --project src/ContentProducer.Worker -- run-once --skip-instagram --skip-telegram
```

In this mode, `INSTAGRAM_ACCESS_TOKEN` and `TELEGRAM_BOT_TOKEN` are not needed.

Run the included example article and image without calling OpenAI:

```bash
dotnet run --project src/ContentProducer.Worker -- run-once --use-fixture --skip-instagram
```

This command tests WordPress and Telegram publishing. It needs
`WORDPRESS_APPLICATION_PASSWORD` and `TELEGRAM_BOT_TOKEN`, but it does not need
`OPENAI_API_KEY`.

Fixture mode can also be enabled in `appsettings.json`:

```json
{
  "ContentSource": {
    "UseFixture": true,
    "FixtureArticleFilePath": "fixtures/article-example.json",
    "FixtureImageFilePath": "fixtures/images/article-image.png"
  }
}
```

PowerShell example:

```powershell
$env:OPENAI_API_KEY="your-api-key"
$env:WORDPRESS_APPLICATION_PASSWORD="your-wordpress-application-password"
dotnet run --project src/ContentProducer.Worker -- run-once --skip-instagram --skip-telegram
```

## WordPress Settings

```json
{
  "WordPress": {
    "SiteUrl": "https://example.com",
    "Username": "wordpress-user",
    "ApplicationPasswordEnvironmentVariable": "WORDPRESS_APPLICATION_PASSWORD",
    "PostStatus": "publish"
  }
}
```

Create a WordPress Application Password for a user that can upload media and
publish posts. Set it on the host:

```bash
export WORDPRESS_APPLICATION_PASSWORD="your-wordpress-application-password"
```

The WordPress site must use HTTPS and expose the REST API.

## Instagram Settings

```json
{
  "Instagram": {
    "GraphApiBaseUrl": "https://graph.facebook.com/",
    "GraphApiVersion": "v25.0",
    "InstagramUserId": "instagram-user-id",
    "AccessTokenEnvironmentVariable": "INSTAGRAM_ACCESS_TOKEN"
  }
}
```

Set the access token on the host:

```bash
export INSTAGRAM_ACCESS_TOKEN="your-instagram-access-token"
```

The Instagram account must be eligible for publishing through the official Meta
API. The access token must include the permissions required for content
publishing.

## Telegram Settings

```json
{
  "Telegram": {
    "BotApiBaseUrl": "https://api.telegram.org/",
    "BotTokenEnvironmentVariable": "TELEGRAM_BOT_TOKEN",
    "ChannelChatId": "@your-channel-username"
  }
}
```

Create a bot token:

1. Open Telegram and message `@BotFather`.
2. Run `/newbot`.
3. Choose the bot display name and username.
4. Copy the generated bot token.

Set the token on the host:

```bash
export TELEGRAM_BOT_TOKEN="your-telegram-bot-token"
```

Add the bot to the target channel and allow it to post messages. `ChannelChatId`
can be a public channel username such as `@recompile_ir` or a numeric channel
ID.

The service uses `sendPhoto` to publish the image and caption.

## Practical Notes

- WordPress media URLs must be publicly reachable without authentication.
- Instagram and Telegram use the public image URL returned by WordPress.
- Tokens and application passwords must not be committed to Git.
