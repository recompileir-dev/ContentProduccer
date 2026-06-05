# مستندات پروژه Content Producer

Content Producer یک Worker سرویس با .NET است که تولید و انتشار محتوای فارسی را
به‌صورت زمان‌بندی‌شده یا دستی اجرا می‌کند. سامانه از Providerهای مختلف برای
تولید مقاله و تصویر استفاده می‌کند و خروجی را به وردپرس، تلگرام و در صورت وجود
تصویر به اینستاگرام منتشر می‌کند.

## وضعیت فعلی پروژه

- اجرای روزانه با `SchedulerAgent` و قابلیت اجرای دستی با `run-once`
- تولید مقاله با Providerهای `OpenAI`، `Groq` یا `Fixture`
- پیدا کردن یا تولید تصویر شاخص با `GoogleImages`، `OpenAI`، `Pollinations` یا `Fixture`
- امکان غیرفعال‌کردن تصویر با `ImageProvider: None`
- انتشار مقاله در وردپرس با دسته‌بندی قابل تنظیم، تصویر شاخص، excerpt و لینک‌های داخلی
- انتشار خلاصه و لینک مطلب در تلگرام، همراه تصویر در صورت وجود
- انتشار پست تک‌تصویری در اینستاگرام وقتی تصویر عمومی از وردپرس در دسترس باشد
- تست لوکال با fixture بدون نیاز به اکانت LLM
- استقرار با Docker Compose روی سرور لینوکس

## اسناد اصلی

- [تعریف و محدوده پروژه](./project-overview.md)
- [معماری فعلی و مسیر تکامل](./architecture.md)
- [جریان تولید و انتشار محتوا](./content-workflow.md)
- [Providerهای تولید محتوا: OpenAI، Groq، GoogleImages، Pollinations، Fixture و None](./content-providers.md)
- [انتشار خودکار در WordPress، Instagram و Telegram](./publishing.md)
- [تنظیمات انتشار و سئو وردپرس](./wordpress-seo.md)
- [استقرار پروژه با Docker روی سرور لینوکس](./docker-deployment.md)
- [راهنمای قدم‌به‌قدم انتقال پروژه به سرور](./server-deployment-step-by-step-fa.md)
- [روایت قدم‌به‌قدم پروژه برای ارائه](./project-journey-presentation-fa.md)
- [نقشه راه توسعه](./roadmap.md)

## فایل‌ها و تنظیمات مهم

- `src/ContentProducer.Worker/appsettings.json`: تنظیمات پیش‌فرض پروژه
- `.env.example`: نمونه متغیرهای محیطی برای Docker و سرور
- `src/ContentProducer.Worker/prompts/article-news-fa.md`: پرامپت مقاله برای OpenAI
- `src/ContentProducer.Worker/prompts/article-news-groq-fa.md`: پرامپت مقاله مخصوص Groq
- `src/ContentProducer.Worker/prompts/article-image-fa.md`: پرامپت تصویر شاخص سایت
- `src/ContentProducer.Worker/fixtures`: مقاله و تصویر نمونه برای تست بدون API
- `local-scripts`: اسکریپت‌های تست لوکال که به‌خاطر داشتن سکرت‌ها وارد Git نمی‌شوند

## اصول نگهداری مستندات

1. هر فیچر جدید باید در README ریشه و سند مرتبط داخل `docs` منعکس شود.
2. هر تنظیم جدید در `appsettings.json` یا `.env.example` باید در مستندات مربوطه توضیح داده شود.
3. سکرت‌ها، پسوردها و توکن‌ها هرگز داخل مستندات واقعی commit نشوند.
4. اگر رفتار پروژه تغییر کرد، سندهای `content-workflow.md` و `architecture.md` باید هم‌زمان به‌روزرسانی شوند.
