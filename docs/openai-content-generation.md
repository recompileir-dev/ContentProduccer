# تولید مقاله و تصاویر با OpenAI

## جریان اجرا

در هر اجرای زمان‌بندی‌شده:

1. فایل پرامپ مقاله از روی هاست خوانده می‌شود.
2. پرامپ به OpenAI Responses API ارسال می‌شود.
3. در صورت فعال بودن `EnableWebSearch`، مدل برای تحقیق درباره خبرهای تازه از ابزار جست‌وجوی وب استفاده می‌کند.
4. مقاله تولیدشده به سرویس ساخت تصویر داده می‌شود.
5. چند تصویر هماهنگ برای کاروسل اینستاگرام با OpenAI Image API ساخته می‌شوند.
6. مقاله شامل عنوان، بدنه HTML و کپشن اینستاگرام به‌صورت ساخت‌یافته دریافت می‌شود.
7. مقاله و تصاویر روی دیسک Worker ذخیره نمی‌شوند و مستقیماً به سرویس‌های انتشار داده می‌شوند.

## تنظیم کلید API

کلید API نباید داخل مخزن Git یا فایل `appsettings.json` ثبت شود. روی هاست لینوکسی متغیر محیطی زیر را تنظیم کنید:

```bash
export OPENAI_API_KEY="your-api-key"
```

استفاده رایگان از ChatGPT به‌معنی اعتبار رایگان OpenAI API نیست. مدل‌های کوچک‌تر هزینه را کاهش می‌دهند، اما برای اجرای API همچنان باید پروژه API دارای اعتبار یا Billing فعال باشد.

نام متغیر محیطی از طریق `OpenAI:ApiKeyEnvironmentVariable` قابل تغییر است. برای محیط‌هایی که امکان تعریف متغیر محیطی ندارند، تنظیم `OpenAI:ApiKey` نیز پشتیبانی می‌شود، اما فایل حاوی آن نباید وارد Git شود.

## تنظیمات

```json
{
  "OpenAI": {
    "ApiKeyEnvironmentVariable": "OPENAI_API_KEY",
    "BaseUrl": "https://api.openai.com/v1/",
    "ArticleModel": "gpt-5-mini",
    "ImageModel": "gpt-image-1-mini",
    "EnableWebSearch": false,
    "ImageCount": 2,
    "ImageSize": "1024x1024",
    "ImageQuality": "low",
    "PromptFilePath": "prompts/article-simple-fa.md"
  }
}
```

مسیرهای نسبی نسبت به پوشه اجرای Worker محاسبه می‌شوند. مسیر مطلق نیز برای فایل پرامپ و پوشه خروجی قابل استفاده است.

## فایل نمونه پرامپ

پرامپ ساده برای تست در مسیر زیر قرار دارد:

```text
src/ContentProducer.Worker/prompts/article-simple-fa.md
```

پرامپ کامل خبری در مسیر زیر حفظ شده است:

```text
src/ContentProducer.Worker/prompts/article-news-fa.md
```

برای انتخاب پرامپ فقط مقدار `OpenAI:PromptFilePath` را در `appsettings.json` تغییر دهید. برای پرامپ خبری، `EnableWebSearch` را نیز روی `true` قرار دهید. این فایل‌ها هنگام Build و Publish همراه برنامه کپی می‌شوند و می‌توان آن‌ها را روی هاست بدون تغییر کد ویرایش کرد.

## خروجی ساخت‌یافته مقاله

پرامپ باید خروجی JSON با فیلدهای زیر تولید کند:

- `title`: عنوان پست وردپرس
- `articleHtml`: بدنه HTML مقاله برای وردپرس
- `instagramCaption`: خلاصه چند خطی مناسب کپشن اینستاگرام

تصاویر در حافظه برنامه نگهداری می‌شوند و مستقیماً به Media Library وردپرس آپلود می‌شوند.
