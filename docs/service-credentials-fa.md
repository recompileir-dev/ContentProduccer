# راهنمای تهیه کلیدها و اطلاعات اتصال سرویس‌ها

این سند توضیح می‌دهد برای اجرای Content Producer باید چه کلیدها، پسوردها و شناسه‌هایی
از سرویس‌های خارجی بگیریم و هرکدام را در کدام متغیر محیطی قرار دهیم.

> نکته امنیتی: مقدارهای واقعی را فقط در `.env` سرور یا `local-scripts/.env.txt`
> محلی نگه دار. هیچ API key، token یا Application Password را داخل Git commit نکن.

## خلاصه متغیرهای لازم

```dotenv
OPENAI_API_KEY=...
GROQ_API_KEY=...

WORDPRESS_SITE_URL=https://example.com/
WORDPRESS_USERNAME=...
WORDPRESS_APPLICATION_PASSWORD=...
WORDPRESS_CATEGORY_ID=50

TELEGRAM_BOT_TOKEN=...
TELEGRAM_CHANNEL_CHAT_ID=-1003717164123
```

اگر Provider مربوطه فعال نباشد، کلید همان Provider لازم نیست. برای مثال اگر
`LLM_PROVIDER=Groq` و `IMAGE_PROVIDER=SourcePageImages` باشد، OpenAI API key لازم
نیست.

## ۱. وردپرس: Application Password برای REST API

برای ارسال پست و آپلود تصویر، پروژه از WordPress REST API استفاده می‌کند. بهترین
روش احراز هویت برای این سناریو، Application Password خود وردپرس است؛ یعنی پسورد
اصلی کاربر را به برنامه نمی‌دهیم، بلکه یک پسورد مخصوص همین برنامه می‌سازیم.

### ساخت Application Password

1. وارد پنل مدیریت وردپرس شو.
2. از منوی کاربران، پروفایل کاربری را باز کن که قرار است پست بسازد.
3. بهتر است این کاربر نقش `Administrator` یا دست‌کم نقشی با اجازه `publish_posts`
   و `upload_files` داشته باشد.
4. در صفحه پروفایل، بخش **Application Passwords** را پیدا کن.
5. برای نام برنامه چیزی مثل `ContentProducer Production` وارد کن.
6. روی ساخت/Generate کلیک کن.
7. پسورد تولیدشده را همان لحظه کپی کن؛ معمولاً بعداً دوباره کامل نمایش داده نمی‌شود.
8. مقدارها را در `.env` بگذار:

```dotenv
WORDPRESS_SITE_URL=https://your-site.com/
WORDPRESS_USERNAME=your-wordpress-user
WORDPRESS_APPLICATION_PASSWORD=xxxx xxxx xxxx xxxx xxxx xxxx
WORDPRESS_CATEGORY_ID=50
```

در کد، username و application password با Basic Authentication به REST API وردپرس
فرستاده می‌شوند.

### تست سریع وردپرس

```bash
curl -u "$WORDPRESS_USERNAME:$WORDPRESS_APPLICATION_PASSWORD" \
  "$WORDPRESS_SITE_URL/wp-json/wp/v2/users/me?context=edit"
```

اگر احراز هویت درست باشد باید اطلاعات کاربر را بگیری. اگر `401` یا `403` دیدی،
مشکل معمولاً یکی از موارد زیر است.

### خطاهای احتمالی افزونه‌های امنیتی و هاست

- REST API غیرفعال شده باشد یا مسیر `/wp-json/` توسط افزونه امنیتی بسته شده باشد.
- افزونه امنیتی، WAF یا Cloudflare درخواست‌های `POST` به `/wp-json/wp/v2/posts` یا
  `/wp-json/wp/v2/media` را بلاک کند.
- هدر `Authorization` توسط هاست یا وب‌سرور حذف شود؛ در این حالت وردپرس Basic Auth
  را نمی‌بیند.
- کاربر انتخاب‌شده اجازه ساخت پست یا آپلود media نداشته باشد.
- Application Password برای کاربر غیرفعال شده باشد.
- سایت HTTPS نداشته باشد یا redirectهای اشتباه باعث حذف header شوند.
- افزونه امنیتی فقط login صفحه مدیریت را مجاز می‌داند، اما REST API را محدود کرده است.

برای عیب‌یابی:

1. اول `/wp-json/` را در مرورگر باز کن و مطمئن شو REST API جواب می‌دهد.
2. تست `users/me` بالا را اجرا کن.
3. اگر media upload خطا داد، endpoint زیر باید برای کاربر مجاز باشد:
   `/wp-json/wp/v2/media`
4. موقتاً ruleهای امنیتی مربوط به REST API را برای تست خاموش کن یا IP سرور را
   allowlist کن.
5. اگر هاست Apache است و `Authorization` حذف می‌شود، معمولاً باید تنظیم rewrite یا
   server config اصلاح شود.

منابع:

- WordPress Application Passwords:
  <https://developer.wordpress.org/advanced-administration/security/application-passwords/>
- WordPress REST API:
  <https://developer.wordpress.org/rest-api/>

## ۲. تلگرام: ساخت Bot و ارسال به Channel

برای تلگرام، پروژه از Bot API استفاده می‌کند. Bot یک token دارد و باید عضو کانالی
باشد که قرار است پست در آن منتشر شود.

### ساخت Bot و گرفتن token

1. در Telegram به `@BotFather` پیام بده.
2. دستور `/newbot` را بفرست.
3. یک نام نمایشی برای bot انتخاب کن.
4. یک username انتخاب کن که با `bot` تمام شود؛ مثلا `content_producer_bot`.
5. BotFather یک token می‌دهد که معمولاً شبیه این است:

```text
1234567890:AAExampleSecretToken
```

6. این token را در `.env` بگذار:

```dotenv
TELEGRAM_BOT_TOKEN=1234567890:AAExampleSecretToken
```

### اتصال Bot به کانال

1. کانال تلگرام را بساز یا کانال موجود را باز کن.
2. Bot را به کانال اضافه کن.
3. بهتر است Bot را Administrator کنی و اجازه ارسال پیام/post بدهی.
4. اگر کانال public است، می‌توانی از username کانال استفاده کنی:

```dotenv
TELEGRAM_CHANNEL_CHAT_ID=@your_channel_username
```

5. اگر کانال private است، باید chat id عددی کانال را بگذاری؛ معمولاً با `-100`
   شروع می‌شود:

```dotenv
TELEGRAM_CHANNEL_CHAT_ID=-1003717164123
```

در این پروژه اگر تصویر وجود داشته باشد تلگرام با `sendPhoto` پست می‌فرستد؛ اگر
تصویر نباشد با `sendMessage` فقط متن و لینک را ارسال می‌کند.

### تست سریع تلگرام

```bash
curl "https://api.telegram.org/bot$TELEGRAM_BOT_TOKEN/getMe"
```

برای تست ارسال پیام:

```bash
curl -X POST "https://api.telegram.org/bot$TELEGRAM_BOT_TOKEN/sendMessage" \
  -d "chat_id=$TELEGRAM_CHANNEL_CHAT_ID" \
  -d "text=Content Producer test"
```

خطاهای رایج:

- `Forbidden: bot is not a member of the channel chat`: Bot عضو کانال نیست.
- `chat not found`: مقدار `TELEGRAM_CHANNEL_CHAT_ID` اشتباه است یا Bot به کانال
  دسترسی ندارد.
- token را با `bot` اول آن در env نگذار؛ فقط خود token را بگذار. کد خودش مسیر
  `/bot{token}/...` را می‌سازد.

منابع:

- Telegram Bot API:
  <https://core.telegram.org/bots/api>
- BotFather:
  <https://core.telegram.org/bots/features#botfather>

## ۳. OpenAI: ساخت API Key

اکانت ChatGPT رایگان با API یکی نیست. برای استفاده از OpenAI در این پروژه، باید
در پلتفرم API کلید بسازی و حساب API دسترسی/billing لازم را داشته باشد.

### ساخت کلید

1. وارد OpenAI Platform شو:
   <https://platform.openai.com/>
2. به بخش API keys برو:
   <https://platform.openai.com/api-keys>
3. روی **Create new secret key** کلیک کن.
4. اگر پروژه/Project انتخاب لازم دارد، project مناسب را انتخاب کن.
5. کلید را همان لحظه کپی کن؛ معمولاً مقدار کامل secret key بعداً دوباره نمایش داده
   نمی‌شود.
6. در `.env` قرار بده:

```dotenv
OPENAI_API_KEY=sk-...
```

در این پروژه OpenAI برای دو مسیر می‌تواند استفاده شود:

- `LLM_PROVIDER=OpenAI`: تولید مقاله با OpenAI
- `IMAGE_PROVIDER=OpenAI`: تولید تصویر شاخص با OpenAI

اگر فقط Groq برای مقاله و `SourcePageImages` برای تصویر استفاده شود، `OPENAI_API_KEY`
لازم نیست.

### خطاهای رایج OpenAI

- `401`: کلید اشتباه است، حذف شده، یا با organization/project فعلی سازگار نیست.
- `429`: ممکن است quota، rate limit یا billing مشکل داشته باشد.
- حساب ChatGPT Plus یا رایگان الزاماً به معنی اعتبار API نیست؛ API billing جداست.
- اگر کلید لو رفت، آن را revoke/delete کن و یک کلید جدید بساز.

منابع:

- راهنمای رسمی OpenAI برای API key:
  <https://help.openai.com/en/articles/4936850-where-do-i-find-my-openai-api-key>
- احراز هویت OpenAI API:
  <https://platform.openai.com/docs/api-reference/authentication>

## ۴. Groq: ساخت API Key

در این پروژه Groq برای تولید مقاله استفاده می‌شود. Groq تولید تصویر ندارد؛ یعنی
اگر `LLM_PROVIDER=Groq` باشد، برای تصویر باید یکی از Providerهای دیگر مثل
`SourcePageImages`، `Pollinations`، `OpenAI`، `Fixture` یا `None` را انتخاب کنیم.

### ساخت کلید

1. وارد Groq Console شو:
   <https://console.groq.com/>
2. اگر حساب نداری، ثبت‌نام/ورود را انجام بده.
3. به بخش API Keys برو:
   <https://console.groq.com/keys>
4. یک API key جدید بساز.
5. کلید را همان لحظه کپی کن.
6. در `.env` قرار بده:

```dotenv
GROQ_API_KEY=gsk_...
LLM_PROVIDER=Groq
```

تنظیم‌های مهم فعلی پروژه:

```dotenv
GROQ_ENABLE_WEB_RESEARCH=true
GROQ_RESEARCH_MODEL=groq/compound-mini
GROQ_WRITER_MODEL=openai/gpt-oss-120b
GROQ_ENABLE_WRITER_FALLBACK=true
GROQ_FALLBACK_WRITER_MODEL=llama-3.3-70b-versatile
```

نکته مهم: مدل `openai/gpt-oss-120b` در این پروژه از طریق API خود Groq صدا زده
می‌شود و به `GROQ_API_KEY` نیاز دارد، نه `OPENAI_API_KEY`.

### خطاهای رایج Groq

- `413 Request Entity Too Large`: request یا خروجی ابزار research بزرگ شده است.
  پروژه برای این مورد research prompt را کوتاه می‌کند و در صورت نیاز بدون research
  ادامه می‌دهد.
- `json_validate_failed`: مدل خروجی JSON معتبر نداده است. پروژه یک fallback writer
  را امتحان می‌کند.
- اگر مقاله خیلی کوتاه باشد، تنظیم `GROQ_REJECT_SHORT_ARTICLES=true` باعث رد شدن
  خروجی می‌شود.

منابع:

- مستندات Groq:
  <https://console.groq.com/docs>
- کلیدهای Groq:
  <https://console.groq.com/keys>

## چک‌لیست نهایی قبل از اجرای production

- `.env` روی سرور ساخته شده و `chmod 600 .env` دارد.
- هیچ secret واقعی در Git نیست.
- `WORDPRESS_SITE_URL` با `/wp-json/` جواب می‌دهد.
- `WORDPRESS_USERNAME` و `WORDPRESS_APPLICATION_PASSWORD` با endpoint `users/me`
  تست شده‌اند.
- Bot تلگرام عضو کانال است و مجوز ارسال دارد.
- `TELEGRAM_CHANNEL_CHAT_ID` برای کانال private با `-100` شروع می‌شود.
- فقط Providerهایی که واقعاً استفاده می‌شوند key دارند.
- بعد از تغییر `.env`، سرویس restart شده است:

```bash
docker compose up -d --force-recreate
```
