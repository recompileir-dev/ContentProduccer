# راهنمای قدم‌به‌قدم انتقال Content Producer به سرور لینوکس

این راهنما برای استقرار پروژه روی یک سرور اختصاصی لینوکس با Docker Compose نوشته
شده است. مسیر پیشنهادی ابتدا تولید محتوا را بدون انتشار آزمایش می‌کند، سپس وردپرس،
تلگرام و اینستاگرام را مرحله‌به‌مرحله فعال می‌کند.

پروژه هیچ پورت ورودی باز نمی‌کند. سرور فقط باید بتواند از طریق HTTPS به سرویس‌های
Groq یا OpenAI، وردپرس، تلگرام و در صورت نیاز Meta متصل شود.

## 1. اطلاعات مورد نیاز را آماده کن

پیش از ورود به سرور این اطلاعات را داشته باش:

- آدرس سایت وردپرس، مانند `https://example.com/`
- نام کاربری وردپرس دارای مجوز انتشار مطلب
- Application Password همان کاربر وردپرس
- شناسه دسته‌بندی وردپرس
- کلید API سرویس Groq یا OpenAI
- توکن بات و شناسه کانال تلگرام، در صورت فعال‌کردن تلگرام
- Instagram User ID و Access Token، در صورت فعال‌کردن اینستاگرام
- ساعت اجرای روزانه و منطقه زمانی، مانند `08:00:00` و `Asia/Tehran`

کلیدها، توکن‌ها و پسوردها را داخل Git یا فایل‌های مستندات قرار نده.

## 2. اتصال به سرور و به‌روزرسانی سیستم

با SSH وارد سرور شو:

```bash
ssh your-user@your-server-ip
```

برای Ubuntu یا Debian:

```bash
sudo apt update
sudo apt upgrade -y
sudo apt install -y git ca-certificates curl
```

## 3. نصب Docker Engine و Docker Compose

برای سرور production از مخزن رسمی Docker استفاده کن. دستورهای دقیق ممکن است با
نسخه توزیع تغییر کنند، بنابراین صفحه رسمی مربوط به سیستم‌عامل را نیز بررسی کن:

- Ubuntu: <https://docs.docker.com/engine/install/ubuntu/>
- Debian: <https://docs.docker.com/engine/install/debian/>

نمونه زیر برای Ubuntu است:

```bash
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
  -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc

echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] \
  https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "${UBUNTU_CODENAME:-$VERSION_CODENAME}") stable" \
  | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io \
  docker-buildx-plugin docker-compose-plugin
```

نصب را بررسی کن:

```bash
sudo systemctl status docker
sudo docker run --rm hello-world
sudo docker compose version
```

اختیاری: برای اجرای Docker بدون `sudo`، کاربر فعلی را به گروه Docker اضافه کن:

```bash
sudo usermod -aG docker "$USER"
newgrp docker
```

عضویت در گروه Docker دسترسی سطح بالا به سرور می‌دهد؛ فقط کاربران قابل اعتماد را
به این گروه اضافه کن.

## 4. دریافت پروژه از GitHub

```bash
sudo mkdir -p /opt/content-producer
sudo chown "$USER":"$USER" /opt/content-producer

git clone https://github.com/recompileir-dev/ContentProduccer.git \
  /opt/content-producer

cd /opt/content-producer
```

اگر مخزن دوباره private شد، به‌جای پسورد GitHub از SSH key یا Personal Access
Token استفاده کن.

## 5. ساخت فایل تنظیمات خصوصی

فایل نمونه را به `.env` تبدیل کن:

```bash
cp .env.example .env
chmod 600 .env
nano .env
```

فایل `.env` در Git نادیده گرفته می‌شود. مقدارهای واقعی را فقط روی سرور نگه دار.

برای شروع با Groq، بدون تصویر و بدون انتشار، این مقدارها را تنظیم کن:

```dotenv
TZ=Asia/Tehran
SCHEDULER_START_AT=08:00:00
SCHEDULER_TIME_ZONE_ID=Asia/Tehran

LLM_PROVIDER=Groq
IMAGE_PROVIDER=None
GROQ_API_KEY=replace-with-real-groq-api-key
GROQ_WRITER_MODEL=openai/gpt-oss-120b
GROQ_MAX_COMPLETION_TOKENS=5000
GROQ_ENABLE_WRITER_FALLBACK=true
GROQ_FALLBACK_WRITER_MODEL=llama-3.3-70b-versatile
GROQ_FALLBACK_MAX_COMPLETION_TOKENS=4500
GROQ_ARTICLE_PROMPT_FILE_PATH=prompts/article-news-groq-fa.md

WORDPRESS_SITE_URL=https://example.com/
WORDPRESS_USERNAME=wordpress-user
WORDPRESS_APPLICATION_PASSWORD=replace-with-wordpress-application-password
WORDPRESS_CATEGORY_ID=50
WORDPRESS_REQUIRE_CATEGORY_ID=true

PUBLISH_TO_WORDPRESS=false
PUBLISH_TO_INSTAGRAM=false
PUBLISH_TO_TELEGRAM=false
```

نکته‌ها:

- مدل `openai/gpt-oss-120b` در این پروژه از API و کلید Groq استفاده می‌کند.
- Groq تولید تصویر ندارد؛ برای اجرای بدون OpenAI مقدار `IMAGE_PROVIDER=None` بماند.
- مقدار `WORDPRESS_CATEGORY_ID` باید شناسه واقعی دسته‌بندی مقصد در وردپرس باشد؛
  در سایت فعلی نمونه، مقدار `50` برای دسته «مجله» استفاده شده است.
- اگر OpenAI باید تصویر بسازد، `IMAGE_PROVIDER=OpenAI` و `OPENAI_API_KEY` را تنظیم کن.
- مقدار `WORDPRESS_CATEGORY_ID` باید شناسه عددی دسته‌بندی واقعی وردپرس باشد.
  در پنل وردپرس به **نوشته‌ها > دسته‌ها** برو، دسته را باز کن و مقدار عددی
  `tag_ID` را از آدرس مرورگر بردار. برای مثال `tag_ID=50` یعنی شناسه دسته `50` است.

## 6. بررسی تنظیمات Compose و ساخت Image

این دستور ساختار Compose را اعتبارسنجی می‌کند، اما خروجی آن ممکن است شامل مقادیر
محیطی باشد؛ خروجی را در جایی عمومی منتشر نکن:

```bash
docker compose config --quiet
```

Image را بساز:

```bash
docker compose build
```

## 7. تست تولید محتوا بدون انتشار

در حالی که هر سه مقدار `PUBLISH_TO_*` برابر `false` هستند، یک اجرای دستی انجام بده:

```bash
docker compose run --rm content-producer run-once
```

در خروجی باید این موارد را ببینی:

- Provider انتخاب‌شده و مسیر پرامپ
- انجام پژوهش Groq یا Web Search سرویس انتخاب‌شده
- تولید مقاله
- پیام غیرفعال‌بودن انتشار وردپرس

اگر Groq مقاله‌ای کوتاه‌تر از حد تعیین‌شده تولید کند، سرویس آن را رد می‌کند و
مقاله ضعیف منتشر نمی‌شود.

## 8. فعال‌کردن و تست وردپرس

در `.env` فقط وردپرس را فعال کن:

```dotenv
PUBLISH_TO_WORDPRESS=true
PUBLISH_TO_INSTAGRAM=false
PUBLISH_TO_TELEGRAM=false
```

سپس اجرا کن:

```bash
docker compose run --rm content-producer run-once
```

بعد از موفقیت، مطلب جدید را در پنل وردپرس بررسی کن. اگر `IMAGE_PROVIDER=None`
باشد، مطلب بدون تصویر شاخص منتشر می‌شود.

## 9. فعال‌کردن و تست تلگرام

بات را به کانال اضافه کن و مجوز ارسال پست بده. سپس در `.env` تنظیم کن:

```dotenv
TELEGRAM_BOT_TOKEN=replace-with-real-telegram-bot-token
TELEGRAM_CHANNEL_CHAT_ID=@your-channel-username
PUBLISH_TO_TELEGRAM=true
```

اجرای دستی:

```bash
docker compose run --rm content-producer run-once
```

وقتی تصویر وجود نداشته باشد، تلگرام متن خلاصه و لینک وردپرس را ارسال می‌کند.

## 10. فعال‌کردن اینستاگرام در صورت نیاز

اینستاگرام برای پست Feed به تصویر نیاز دارد. ابتدا `IMAGE_PROVIDER=OpenAI` و
`OPENAI_API_KEY` را تنظیم کن، سپس:

```dotenv
INSTAGRAM_USER_ID=replace-with-instagram-user-id
INSTAGRAM_ACCESS_TOKEN=replace-with-instagram-access-token
PUBLISH_TO_INSTAGRAM=true
```

دوباره اجرای دستی را انجام بده و نتیجه را در حساب اینستاگرام بررسی کن.

## 11. راه‌اندازی Scheduler دائمی

پس از موفقیت تست‌ها، سرویس دائمی را بالا بیاور:

```bash
docker compose up -d
```

وضعیت و لاگ‌ها:

```bash
docker compose ps
docker compose logs -f --tail=200 content-producer
```

به‌دلیل `restart: unless-stopped`، کانتینر پس از reboot سرور دوباره اجرا می‌شود.

## 12. تغییر ساعت یا پرامپ

برای تغییر ساعت، `.env` را ویرایش کن:

```dotenv
SCHEDULER_START_AT=08:00:00
SCHEDULER_TIME_ZONE_ID=Asia/Tehran
```

پرامپ‌ها از پوشه زیر به‌صورت read-only داخل کانتینر mount می‌شوند:

```text
src/ContentProducer.Worker/prompts/
```

پس از تغییر `.env` یا پرامپ، سرویس را بازسازی یا restart کن:

```bash
docker compose up -d
```

## 13. به‌روزرسانی نسخه پروژه

```bash
cd /opt/content-producer
git pull
docker compose up -d --build
docker compose logs -f --tail=200 content-producer
```

فایل `.env` با `git pull` حذف یا جایگزین نمی‌شود، چون در Git ثبت نشده است.

## 14. دستورات روزمره

مشاهده لاگ‌های اخیر:

```bash
docker compose logs --tail=300 content-producer
```

توقف سرویس:

```bash
docker compose down
```

راه‌اندازی دوباره:

```bash
docker compose up -d
```

مشاهده مصرف منابع:

```bash
docker stats content-producer
```

## 15. نکات امنیتی و نگهداری

- فایل `.env` را با مجوز `600` نگه دار و از آن نسخه پشتیبان رمزگذاری‌شده داشته باش.
- کلیدها و Application Passwordها را دوره‌ای تعویض کن.
- فقط دسترسی خروجی HTTPS لازم است؛ برای این Worker پورت ورودی باز نکن.
- ساعت و منطقه زمانی سرور را صحیح نگه دار.
- Docker، سیستم‌عامل و image پروژه را به‌روزرسانی کن.
- لاگ‌ها را پس از هر اجرای زمان‌بندی‌شده بررسی کن، مخصوصاً خطاهای API، وردپرس و کیفیت مقاله.

## منابع رسمی Docker

- <https://docs.docker.com/engine/install/>
- <https://docs.docker.com/engine/install/ubuntu/>
- <https://docs.docker.com/engine/install/debian/>
- <https://docs.docker.com/compose/install/linux/>
