# تولید مقاله و تصاویر با OpenAI

## جریان اجرا

در هر اجرای زمان‌بندی‌شده:

1. فایل پرامپ مقاله از روی هاست خوانده می‌شود.
2. پرامپ به OpenAI Responses API ارسال می‌شود.
3. در صورت فعال بودن `EnableWebSearch`، مدل برای تحقیق درباره خبرهای تازه از ابزار جست‌وجوی وب استفاده می‌کند.
4. مقاله تولیدشده به سرویس ساخت تصویر داده می‌شود.
5. چند تصویر هماهنگ برای کاروسل اینستاگرام با OpenAI Image API ساخته می‌شوند.
6. مقاله و تصاویر در یک پوشه زمان‌دار داخل مسیر خروجی ذخیره می‌شوند.

## تنظیم کلید API

کلید API نباید داخل مخزن Git یا فایل `appsettings.json` ثبت شود. روی هاست لینوکسی متغیر محیطی زیر را تنظیم کنید:

```bash
export OPENAI_API_KEY="your-api-key"
```

نام متغیر محیطی از طریق `OpenAI:ApiKeyEnvironmentVariable` قابل تغییر است. برای محیط‌هایی که امکان تعریف متغیر محیطی ندارند، تنظیم `OpenAI:ApiKey` نیز پشتیبانی می‌شود، اما فایل حاوی آن نباید وارد Git شود.

## تنظیمات

```json
{
  "OpenAI": {
    "ApiKeyEnvironmentVariable": "OPENAI_API_KEY",
    "BaseUrl": "https://api.openai.com/v1/",
    "ArticleModel": "gpt-5.5",
    "ImageModel": "gpt-image-2",
    "EnableWebSearch": true,
    "ImageCount": 4,
    "ImageSize": "1024x1024",
    "ImageQuality": "medium",
    "PromptFilePath": "prompts/article-news-fa.md",
    "OutputDirectory": "output"
  }
}
```

مسیرهای نسبی نسبت به پوشه اجرای Worker محاسبه می‌شوند. مسیر مطلق نیز برای فایل پرامپ و پوشه خروجی قابل استفاده است.

## فایل نمونه پرامپ

فایل نمونه در مسیر زیر قرار دارد:

```text
src/ContentProducer.Worker/prompts/article-news-fa.md
```

این فایل هنگام Build و Publish همراه برنامه کپی می‌شود و می‌توان آن را روی هاست بدون تغییر کد ویرایش کرد.

## خروجی

هر اجرا پوشه‌ای شبیه نمونه زیر می‌سازد:

```text
output/
  20260603-080000/
    article.md
    carousel-01.png
    carousel-02.png
    carousel-03.png
    carousel-04.png
```

ارسال مقاله به وردپرس و تصاویر به Instagram Graph API در مرحله بعدی اضافه خواهد شد.
