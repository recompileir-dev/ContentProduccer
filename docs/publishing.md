# انتشار خودکار در وردپرس و اینستاگرام

## جریان انتشار

1. مقاله و کپشن اینستاگرام از OpenAI دریافت می‌شوند.
2. تصاویر کاروسل در حافظه برنامه تولید می‌شوند.
3. همه تصاویر مستقیماً به Media Library وردپرس آپلود می‌شوند.
4. مقاله وردپرس با تصویر اول به‌عنوان Featured Image منتشر می‌شود.
5. URL عمومی تصاویر وردپرس برای ساخت Media Containerهای اینستاگرام استفاده می‌شوند.
6. تصاویر به‌صورت کاروسل همراه کپشن تولیدشده در اینستاگرام منتشر می‌شوند.

فایل مقاله و تصاویر روی دیسک Worker ذخیره نمی‌شوند. با این حال تصاویر در Media Library وردپرس باقی می‌مانند، چون Instagram Graph API برای دریافت تصاویر به URL عمومی نیاز دارد.

## اجرای تستی بدون Scheduler

برای اجرای یک‌باره کل جریان:

```bash
dotnet run --project src/ContentProducer.Worker -- run-once
```

برای تست فقط OpenAI و WordPress و رد کردن Instagram:

```bash
dotnet run --project src/ContentProducer.Worker -- run-once --skip-instagram
```

در حالت دوم نیازی به `INSTAGRAM_ACCESS_TOKEN` نیست.

در ویندوز PowerShell:

```powershell
$env:OPENAI_API_KEY="your-api-key"
$env:WORDPRESS_APPLICATION_PASSWORD="your-wordpress-application-password"
dotnet run --project src/ContentProducer.Worker -- run-once --skip-instagram
```

## تنظیمات وردپرس

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

در پنل وردپرس برای کاربری که اجازه آپلود رسانه و انتشار نوشته دارد، یک Application Password بسازید. سپس آن را روی هاست به‌صورت متغیر محیطی تنظیم کنید:

```bash
export WORDPRESS_APPLICATION_PASSWORD="your-wordpress-application-password"
```

سایت وردپرس باید HTTPS داشته باشد و REST API آن در دسترس باشد.

## تنظیمات اینستاگرام

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

توکن دسترسی را روی هاست تنظیم کنید:

```bash
export INSTAGRAM_ACCESS_TOKEN="your-instagram-access-token"
```

حساب اینستاگرام باید شرایط انتشار از طریق API رسمی متا را داشته باشد و توکن باید مجوزهای لازم برای انتشار محتوا را داشته باشد. نسخه Graph API قابل تنظیم است و باید با نسخه فعال برنامه متا هماهنگ شود.

## نکات عملی

- URL تصاویر وردپرس باید از اینترنت و بدون احراز هویت قابل دسترسی باشد.
- تعداد تصاویر کاروسل باید حداقل دو عدد باشد.
- سرویس پیش از انتشار کاروسل، آماده‌شدن Media Containerهای متا را بررسی می‌کند.
- کلیدها و توکن‌ها نباید داخل Git ثبت شوند.
