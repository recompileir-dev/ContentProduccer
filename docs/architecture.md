# معماری فعلی و مسیر تکامل

## نمای کلی

معماری پروژه از یک Worker ساده شروع شد و حالا به یک workflow قابل تنظیم تبدیل
شده است. هسته سیستم به Provider خاصی وابسته نیست؛ یعنی تولید متن، تولید تصویر و
انتشار در کانال‌ها هرکدام پشت interface و سرویس جدا قرار گرفته‌اند.

```text
SchedulerAgent / run-once
          |
          v
ContentProductionService
          |
          v
ContentGeneratorService
   |                  |
   v                  v
ILlmProvider      IImageProvider
OpenAI/Groq/      GoogleImages/OpenAI/
Fixture           Pollinations/Fixture/None
          |
          v
GeneratedContent
          |
          v
WordPressPublisherService
          |
          +----> InstagramPublisherService
          |
          +----> TelegramPublisherService
```

## اجزای اصلی

### 1. زمان‌بندی و اجرای دستی

`SchedulerAgent` برنامه را در ساعت مشخص‌شده در `Scheduler:StartAt` و منطقه زمانی
`Scheduler:TimeZoneId` اجرا می‌کند. برای تست یا اجرای فوری، دستور زیر scheduler
را دور می‌زند و workflow را یک‌بار اجرا می‌کند:

```bash
dotnet run --project src/ContentProducer.Worker -- run-once
```

### 2. هماهنگ‌کننده تولید و انتشار

`ContentProductionService` مراحل اصلی را مدیریت می‌کند:

1. اعتبارسنجی اتصال وردپرس در صورت فعال بودن انتشار وردپرس
2. تولید مقاله و تصویر از طریق `ContentGeneratorService`
3. آپلود تصویر در وردپرس، اگر تصویر وجود داشته باشد
4. انتشار مقاله در وردپرس
5. ساخت کپشن شبکه‌های اجتماعی با لینک مقاله
6. انتشار در اینستاگرام و تلگرام طبق تنظیمات

در نسخه فعلی، اگر `PublishToWordPress` غیرفعال باشد، انتشار شبکه‌های اجتماعی هم
انجام نمی‌شود؛ چون لینک اصلی مقاله از وردپرس گرفته می‌شود.

### 3. تولید محتوا

`ContentGeneratorService` بر اساس تنظیمات زیر Providerها را انتخاب می‌کند:

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
  }
}
```

Providerهای متن:

- `OpenAI`
- `Groq`
- `Fixture`

Providerهای تصویر:

- `OpenAI`
- `GoogleImages`
- `Pollinations`
- `Fixture`
- `None`

### 4. انتشار در وردپرس

`WordPressPublisherService` از WordPress REST API استفاده می‌کند. این سرویس:

- اتصال و احراز هویت را قبل از تولید محتوا validate می‌کند.
- دسته‌بندی تنظیم‌شده را validate می‌کند.
- در صورت وجود تصویر، آن را در Media Library آپلود می‌کند.
- alt، caption و title تصویر را تنظیم می‌کند.
- مقاله را با دسته‌بندی، excerpt، تصویر شاخص و محتوای HTML منتشر می‌کند.
- به‌صورت پیش‌فرض پست را با وضعیت `draft` می‌سازد.
- بعد از انتشار بررسی می‌کند که دسته‌بندی درست روی پست ثبت شده باشد.

### 5. انتشار در تلگرام

`TelegramPublisherService` از Bot API رسمی تلگرام استفاده می‌کند. اگر تصویر
وجود داشته باشد `sendPhoto` صدا زده می‌شود و اگر تصویر وجود نداشته باشد
`sendMessage` استفاده می‌شود. بنابراین تلگرام با حالت `ImageProvider: None` هم
کار می‌کند.

### 6. انتشار در اینستاگرام

`InstagramPublisherService` از Instagram Graph API استفاده می‌کند. چون API
اینستاگرام برای feed به media نیاز دارد، اگر تصویر تولید نشده باشد انتشار
اینستاگرام skip می‌شود.

### 7. Providerها

هدف از Provider abstraction این است که workflow اصلی به مدل خاصی وابسته نباشد.
برای مثال می‌توان مقاله را با Groq تولید کرد و تصویر را با GoogleImages پیدا کرد، با Pollinations یا OpenAI ساخت، یا برای
تست هر دو را با Fixture جایگزین کرد.

## ساختار پوشه‌های اصلی

```text
src/ContentProducer.Worker/
  Application/              workflow، تولید کپشن و orchestration
  Configuration/            تنظیمات Providerها و اجرای برنامه
  Domain/                   مدل‌های مقاله، تصویر و parser خروجی
  Providers/Abstractions/   قراردادهای LLM و Image Provider
  Providers/OpenAI/         تولید مقاله و تصویر با OpenAI
  Providers/Groq/           تولید مقاله با Groq
  Providers/GoogleImages/   جستجوی تصویر موجود با Google Custom Search
  Providers/Pollinations/   تولید تصویر از endpoint عمومی Pollinations
  Providers/Fixture/        داده نمونه برای تست بدون API
  Providers/None/           غیرفعال‌کردن تولید تصویر
  Publishing/WordPress/     انتشار مقاله و تصویر در وردپرس
  Publishing/Instagram/     انتشار پست تصویری در اینستاگرام
  Publishing/Telegram/      انتشار خلاصه و لینک در تلگرام
  Scheduling/               scheduler روزانه
  prompts/                  پرامپت‌های قابل ویرایش
  fixtures/                 مقاله و تصویر نمونه
```

## محدودیت‌های معماری فعلی

- دیتابیس وجود ندارد؛ بنابراین تاریخچه اجرا، deduplication و retry پایدار نداریم.
- queue وجود ندارد؛ مراحل در یک اجرای خطی انجام می‌شوند.
- مرحله بازبینی انسانی هنوز پیاده‌سازی نشده است.
- اگر وردپرس خاموش باشد، شبکه‌های اجتماعی هم لینک اصلی مطلب را ندارند.
- مانیتورینگ production فعلاً به log محدود است.

## مسیر معماری بعدی

گام بعدی معماری باید اضافه‌کردن یک storage سبک برای `JobRun` و `PublishedContent`
باشد. با این تغییر می‌توان تکرار موضوع را کنترل کرد، خطاها را retry کرد، لینک
پست‌های منتشرشده را نگه داشت و بعداً پنل مدیریت یا مرحله تأیید انسانی ساخت.
