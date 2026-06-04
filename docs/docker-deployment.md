# Docker Deployment

This document describes production deployment on a dedicated Linux server.
The project uses .NET 10 LTS and Docker Compose. The Worker does not expose an
HTTP port because it only runs the scheduler and makes outbound API requests.

## Files

- `Dockerfile`: builds and runs the Worker
- `compose.yaml`: starts one persistent scheduler container
- `.env.example`: template for server settings and secrets
- `.dockerignore`: keeps local and unnecessary files out of the image

The Compose service mounts `src/ContentProducer.Worker/prompts` into
`/app/prompts` as read-only. Prompt files can therefore be edited on the server
without rebuilding the image. Restart the container after changing a prompt.

## 1. Prepare the Server

Install Git, Docker Engine, and the Docker Compose plugin. For Ubuntu or Debian,
use Docker's official installation instructions:

- Ubuntu: <https://docs.docker.com/engine/install/ubuntu/>
- Debian: <https://docs.docker.com/engine/install/debian/>

Verify the installation:

```bash
docker --version
docker compose version
```

Optional: allow the current Linux user to run Docker without `sudo`:

```bash
sudo usermod -aG docker "$USER"
newgrp docker
```

Docker group membership grants powerful access to the server. Only add trusted
users.

## 2. Clone the Project

```bash
sudo mkdir -p /opt/content-producer
sudo chown "$USER":"$USER" /opt/content-producer
git clone https://github.com/recompileir-dev/ContentProduccer.git /opt/content-producer
cd /opt/content-producer
```

## 3. Create Production Settings

Create a private `.env` file from the committed template:

```bash
cp .env.example .env
chmod 600 .env
nano .env
```

Replace every placeholder with the real value. Important settings:

- `OPENAI_API_KEY`
- `GROQ_API_KEY` when `LLM_PROVIDER=Groq`
- `WORDPRESS_SITE_URL`
- `WORDPRESS_USERNAME`
- `WORDPRESS_APPLICATION_PASSWORD`
- `WORDPRESS_CATEGORY_ID`
- `INSTAGRAM_USER_ID`
- `INSTAGRAM_ACCESS_TOKEN`
- `TELEGRAM_BOT_TOKEN`
- `TELEGRAM_CHANNEL_CHAT_ID`
- `SCHEDULER_START_AT`

The `.env` file is ignored by Git. Do not commit, upload, or share it.

For the full news workflow with OpenAI, keep:

```dotenv
LLM_PROVIDER=OpenAI
OPENAI_ENABLE_WEB_SEARCH=true
ARTICLE_PROMPT_FILE_PATH=prompts/article-news-fa.md
```

To generate articles with Groq and images with OpenAI:

```dotenv
LLM_PROVIDER=Groq
IMAGE_PROVIDER=OpenAI
GROQ_MODEL=groq/compound
ARTICLE_PROMPT_FILE_PATH=prompts/article-news-fa.md
```

To temporarily disable a publishing destination:

```dotenv
PUBLISH_TO_INSTAGRAM=false
PUBLISH_TO_TELEGRAM=false
```

## 4. Build and Start the Scheduler

```bash
cd /opt/content-producer
docker compose up -d --build
```

The container starts the scheduler and waits until the configured daily time.
It restarts automatically after a server reboot or an unexpected process exit.

Check its state and logs:

```bash
docker compose ps
docker compose logs -f --tail=200 content-producer
```

The first log messages should show the configured execution time, time zone,
and next scheduled run.

## 5. Run a Manual Test

Run the real workflow immediately instead of waiting for the scheduler:

```bash
docker compose run --rm content-producer run-once
```

Test WordPress and Telegram with fixture content, without OpenAI or Instagram:

```bash
docker compose run --rm content-producer run-once --use-fixture --skip-instagram
```

Test only OpenAI and WordPress:

```bash
docker compose run --rm content-producer run-once --skip-instagram --skip-telegram
```

## 6. Update the Deployment

```bash
cd /opt/content-producer
git pull
docker compose up -d --build
docker compose logs -f --tail=200 content-producer
```

The `.env` file remains on the server because it is not tracked by Git.

## 7. Common Operations

Stop the service:

```bash
docker compose down
```

Restart after changing `.env` or prompt files:

```bash
docker compose up -d
```

View recent errors:

```bash
docker compose logs --tail=300 content-producer
```

## Notes

- No inbound firewall port is required for this Worker.
- The server must allow outbound HTTPS access to the selected LLM provider,
  OpenAI image generation, WordPress, Meta, and Telegram.
- Keep the server clock and time zone correct. The scheduler uses
  `SCHEDULER_TIME_ZONE_ID`, while container logs use `TZ`.
- Back up the `.env` file securely outside Git.
- Update the Docker image regularly to receive .NET and operating system security patches.
