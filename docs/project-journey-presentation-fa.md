# روایت پروژه Content Producer برای ارائه

این سند یک خط زمانی اجرایی از شکل‌گیری پروژه تا وضعیت فعلی است. هدفش این است
که بعداً بتوان از روی آن یک پرزنتیشن جذاب ساخت: چه مسئله‌ای داشتیم، چه تصمیم‌هایی
گرفتیم، چه مشکلاتی پیش آمد، معماری چطور تغییر کرد و قدم‌های بعدی چه هستند.

## خلاصه مدیریتی

Content Producer از یک ایده ساده شروع شد: تولید خودکار محتوای روزانه با هوش
مصنوعی و انتشار آن در وردپرس، اینستاگرام و کانال‌های اجتماعی. مسیر توسعه به‌جای
ساخت یک سیستم بزرگ و پیچیده، با یک MVP واقعی و قابل اجرا شروع شد: Worker
زمان‌بندی‌شده، بدون دیتابیس، بدون lock توزیع‌شده، با قابلیت اجرای دستی برای تست.

در ادامه پروژه از یک Worker ساده به یک سامانه قابل تنظیم تبدیل شد که چند Provider
محتوا دارد، می‌تواند با OpenAI، Groq یا fixture کار کند، خروجی را به وردپرس،
تلگرام و اینستاگرام بفرستد، بدون تصویر هم کار کند، Dockerized شود و روی سرور
لینوکس اجرا شود.

## فهرست درخواست‌های کاربر در این سشن

1. ایجاد پوشه `docs` و ثبت توضیحات اولیه پروژه.
2. اتصال پروژه به مخزن GitHub `recompileir-dev/ContentProduccer`.
3. بررسی مشکل اتصال GitHub private و توضیح اینکه چرا username/password معمولی
   پذیرفته نمی‌شود.
4. تلاش مجدد بعد از public شدن مخزن.
5. طراحی شروع پروژه با یک scheduler روی هاست لینوکسی/cPanel و انتخاب تکنولوژی
   مناسب با توجه به تخصص C#.
6. ساخت MVP scheduler با حداقل نیازها: یک نسخه همزمان، بدون دیتابیس، بدون retry
   پیچیده و بدون lock.
7. ساخت سرویس تولید مقاله از روی فایل prompt و ارسال به ChatGPT/OpenAI.
8. ساخت سرویس تولید تصویر بر اساس مقاله.
9. ساخت prompt نمونه برای مقاله خبری درباره اثر AI در زندگی.
10. افزودن انتشار خودکار به وردپرس و اینستاگرام.
11. تولید caption جداگانه برای اینستاگرام از داخل خروجی مقاله.
12. ارسال مقاله به وردپرس با تصویر اول به‌عنوان تصویر مطلب.
13. ارسال پست اینستاگرام با caption و تصویر/کاروسل.
14. توضیح دقیق محل دریافت API keyها و اطلاعات اتصال OpenAI، WordPress و Instagram.
15. افزودن اجرای دستی `run-once` برای تست لوکال بدون انتظار scheduler.
16. ساخت اسکریپت PowerShell محلی و قرار دادن آن خارج از Git.
17. بررسی خطای OpenAI 429 و توضیح مشکل billing/limit.
18. بازسازی تغییرات بعد از خراب‌شدن فایل‌ها.
19. افزودن انتشار به Telegram channel.
20. توضیح ساخت Telegram bot، افزودن bot به channel و مقدار `ChannelChatId`.
21. توضیح نقش bot ساخته‌شده.
22. تغییر مدل OpenAI برای تست ارزان‌تر و ساخت prompt ساده قابل انتخاب.
23. افزودن fixture برای تست بدون اکانت LLM و استفاده از تصاویر محلی تستی.
24. بررسی خطاهای WordPress 403، 400 و اصلاح authentication و media upload.
25. اصلاح WordPress publishing: فقط یک تصویر برای هر مقاله، حذف citationهای
    خراب، استفاده از تصویر در متن، تنظیم دسته‌بندی و بهبود SEO.
26. توضیح معنی `CategoryId` و به‌روزرسانی fixture با مقاله جدید.
27. حذف مفهوم کاروسل و تبدیل تصویر به یک تصویر شاخص مناسب سایت.
28. تغییر prompt تصویر برای عرض ۷۹۰، تصویر افقی، و اولویت نمایش مردان وقتی موضوع
    مستقیماً درباره زنان نیست.
29. خارج‌کردن prompt تصویر از کد و قرار دادن در فایل قابل ویرایش.
30. تغییر prompt مقاله تا هر بار مقاله کلی درباره AI نسازد و بر اساس ۲ یا ۳ خبر
    تازه بنویسد.
31. بررسی خطاهای Telegram شامل scheme اشتباه token و عضو نبودن bot در channel.
32. Dockerize کردن پروژه برای اجرای روی سرور اختصاصی لینوکس.
33. جدا کردن وابستگی LLM با Interface و تبدیل ChatGPT/OpenAI به یک Provider.
34. افزودن Provider کامل Groq.
35. افزودن Provider fake/fixture.
36. جداسازی تنظیمات Providerها در `appsettings`.
37. بازبینی تسک Providerها، اصلاح اسکریپت‌های لوکال و مرتب‌سازی کلاس‌ها در پوشه‌ها.
38. رفع خطاهای Groq 413 به‌خاطر بزرگ‌بودن request یا ابزارهای Compound.
39. تشخیص اینکه Groq تصویر تولید نمی‌کند و افزودن `ImageProvider=None`.
40. اصلاح workflow بدون تصویر: وردپرس text-only، تلگرام text-only، skip اینستاگرام.
41. خارج‌کردن username وردپرس از `appsettings` و خواندن آن از environment variable.
42. بررسی کوتاه‌بودن خروجی Groq، ساخت prompt اختصاصی Groq و انتخاب مدل مناسب‌تر.
43. افزودن شرط استفاده از منابع منتشرشده از ۱ ژانویه ۲۰۲۶ به بعد.
44. حذف `article-simple-fa.md` و تغییر پیش‌فرض prompt به خبر.
45. نوشتن راهنمای قدم‌به‌قدم استقرار روی سرور لینوکس.
46. ممنوع‌کردن عنوان‌های کلی مثل «تأثیر هوش مصنوعی در زندگی روزمره».
47. ممنوع‌کردن خبرهای خارج از دامنه مجله عمومی فناوری.
48. بررسی مشکل دسته‌بندی وردپرس و کشف اینکه `WORDPRESS_CATEGORY_ID=1` یعنی
    «دسته‌بندی نشده»، در حالی که دسته موردنظر `50` یعنی «مجله» بود.
49. سخت‌گیرانه‌کردن اعتبارسنجی دسته‌بندی وردپرس قبل و بعد از publish.
50. تغییر promptها برای تنوع موضوع: AI، زندگی انسان، برنامه‌نویسی، ابزارهای توسعه،
    شغل‌ها و شیوه کار.
51. جلوگیری از آمدن عدد `۲۰۲۶` در عنوان و SEO مگر وقتی خود خبر واقعاً درباره سال
    ۲۰۲۶ باشد.
52. بررسی خطای Groq `json_validate_failed` و مقاوم‌سازی خروجی JSON با Structured
    Outputs، fallback و تشخیص refusal واقعی.
53. درخواست ساخت همین سند روایی برای ارائه و ادامه مسیر پروژه.

## خط زمانی فنی پروژه

### مرحله ۱: مستندسازی و اتصال GitHub

شروع پروژه با این هدف بود که یک سامانه تولید محتوای خودکار ساخته شود. در همان ابتدا
پوشه `docs` ایجاد شد و اسناد پایه مثل overview، architecture، workflow و roadmap
اضافه شدند. سپس پروژه به GitHub وصل شد.

مشکل مهم این مرحله این بود که GitHub دیگر username/password ساده را برای عملیات Git
قبول نمی‌کند، مخصوصاً برای repositoryهای private. راه‌حل عملی در آن لحظه public کردن
repository و ادامه کار بود، اما برای آینده باید SSH key یا Personal Access Token
در نظر گرفته شود.

خروجی این مرحله:

- مستندات اولیه پروژه
- اتصال GitHub
- تعریف دامنه اولیه سامانه

### مرحله ۲: MVP زمان‌بندی‌شده با .NET Worker

با توجه به اینکه تخصص اصلی کاربر C# است، گزینه طبیعی برای MVP استفاده از .NET Worker
بود. محدودیت محیط hosting هم بررسی شد: cPanel/shared hosting برای سرویس‌های دائمی
مناسب نیست، اما یک سرور اختصاصی لینوکس یا Docker می‌تواند Worker را اجرا کند.

در MVP تصمیم گرفتیم:

- فقط یک scheduler ساده داشته باشیم.
- دیتابیس نداشته باشیم.
- distributed lock نداشته باشیم.
- retry و سیاست خطای پیچیده نداشته باشیم.
- قابلیت اجرای دستی داشته باشیم.

این تصمیم‌ها کمک کردند پروژه سریع به یک نسخه واقعی برسد، نه یک طراحی سنگین و دیررس.

خروجی این مرحله:

- `SchedulerAgent`
- تنظیم ساعت و timezone
- دستور `run-once`

### مرحله ۳: تولید مقاله و تصویر با OpenAI

قدم بعدی تولید محتوا بود. ابتدا سرویس مقاله از روی فایل prompt ساخته شد و سپس سرویس
تصویر اضافه شد. prompt مقاله و prompt تصویر از کد جدا شدند تا بدون تغییر کد قابل
ویرایش باشند.

در طراحی خروجی مقاله، نیازهای بعدی هم دیده شد:

- `title`
- `articleHtml`
- `instagramCaption`
- `focusKeyphrase`
- `seoTitle`
- `metaDescription`
- `references`

این ساختار باعث شد بعداً بتوانیم وردپرس، شبکه‌های اجتماعی و SEO را راحت‌تر تغذیه
کنیم.

مشکل مهم این مرحله OpenAI billing بود. مشخص شد اکانت رایگان ChatGPT به معنی دسترسی
رایگان API نیست. خطای 429 و محدودیت billing باعث شد نیاز به Providerهای جایگزین و
fixture جدی شود.

خروجی این مرحله:

- `OpenAiLlmProvider`
- `OpenAiImageProvider`
- promptهای مقاله و تصویر
- ساختار JSON مقاله

### مرحله ۴: انتشار به WordPress و Instagram

پس از تولید محتوا، انتشار مستقیم اضافه شد. هدف این بود که فایل مقاله و تصویر روی هاست
ذخیره نشوند و مستقیماً به مقصدها ارسال شوند.

برای وردپرس:

- تصویر در Media Library آپلود شد.
- مقاله با REST API منتشر شد.
- تصویر شاخص و تصویر داخل متن پشتیبانی شد.
- excerpt و metadata برای SEO در نظر گرفته شد.

برای Instagram:

- ابتدا ایده carousel مطرح بود.
- بعداً carousel حذف شد و تمرکز روی یک تصویر شاخص مناسب سایت رفت.
- اگر تصویر وجود نداشته باشد، انتشار اینستاگرام skip می‌شود؛ چون API رسمی Instagram
  برای feed post به media نیاز دارد.

مشکلات وردپرس:

- 403 در authentication
- 400 به‌خاطر نبود `Content-Disposition` هنگام آپلود media
- مجوز REST API
- citation placeholderهای تولیدشده توسط مدل
- تصویر آپلود می‌شد ولی در متن استفاده نمی‌شد

راه‌حل‌ها:

- اعتبارسنجی REST API پیش از انتشار
- افزودن Content-Disposition برای media upload
- پاکسازی `:contentReference[...]`
- قرار دادن تصویر داخل HTML مقاله
- افزودن alt text و metadata

خروجی این مرحله:

- `WordPressPublisherService`
- `InstagramPublisherService`
- formatter برای HTML وردپرس
- تنظیمات WordPress، Instagram و Publishing

### مرحله ۵: Telegram Publishing

بعد از وردپرس و اینستاگرام، تلگرام اضافه شد تا خلاصه مقاله و لینک وردپرس در کانال
منتشر شود. برای این کار bot ساخته شد، token از BotFather گرفته شد و bot به کانال
اضافه شد.

مشکلات تلگرام:

- token اشتباه به‌عنوان URL scheme استفاده شده بود.
- bot عضو کانال نبود و خطای 403 می‌داد.
- نبود تصویر در workflow باعث نیاز به پیام text-only شد.

راه‌حل‌ها:

- ساخت URL امن برای Telegram Bot API
- استفاده از `sendPhoto` وقتی تصویر هست
- استفاده از `sendMessage` وقتی تصویر نیست
- توضیح مجوزهای لازم bot در مستندات

خروجی این مرحله:

- `TelegramPublisherService`
- پشتیبانی از image post و text post
- تنظیمات Telegram در `appsettings`

### مرحله ۶: Fixture و تست بدون API

برای تست بدون مصرف اعتبار API، fixture اضافه شد. مقاله نمونه و تصویر نمونه از فایل
خوانده می‌شوند و workflow انتشار را بدون تماس با LLM تست می‌کنند.

این تصمیم بسیار مهم بود چون:

- تست وردپرس و تلگرام از تولید محتوا جدا شد.
- مشکلات انتشار سریع‌تر پیدا شدند.
- توسعه بدون هزینه API ممکن شد.

خروجی این مرحله:

- `FixtureLlmProvider`
- `FixtureImageProvider`
- فایل‌های fixture
- اسکریپت‌های PowerShell محلی در `local-scripts`

### مرحله ۷: Provider Architecture

با جدی‌شدن مشکل مدل‌ها و هزینه‌ها، معماری Provider اضافه شد. به‌جای وابستگی مستقیم
به OpenAI، سیستم به interfaceهای مستقل متکی شد:

- `ILlmProvider`
- `IImageProvider`

Providerهای فعلی:

- OpenAI برای مقاله و تصویر
- Groq برای مقاله
- Fixture برای تست
- None برای غیرفعال‌کردن تصویر

این تغییر یکی از مهم‌ترین نقاط معماری پروژه بود، چون از این مرحله به بعد انتخاب مدل
یا سرویس فقط یک تنظیم بود، نه تغییر کد اصلی workflow.

خروجی این مرحله:

- جداسازی `Application`, `Configuration`, `Domain`, `Providers`, `Publishing`
- Providerهای مستقل
- انتخاب Provider از `appsettings` و env

### مرحله ۸: Groq و چالش‌های آن

Groq به‌عنوان Provider جایگزین اضافه شد، اما چند مشکل مهم داشت:

#### مشکل 413

در ابتدا prompt و خروجی پژوهش برای مدل‌های Compound سنگین بود و خطای
`Request Entity Too Large` ایجاد می‌کرد.

راه‌حل:

- جدا کردن مرحله research و writer
- استفاده از `groq/compound-mini` برای پژوهش
- محدود کردن طول prompt پژوهش
- استفاده از نسخه Basic Search با `Groq-Model-Version`
- ادامه بدون research فقط در خطای 413 کنترل‌شده

#### نبود تولید تصویر

بررسی شد که Groq image generation ندارد. Groq می‌تواند تصویر را تحلیل کند، اما
تصویر تولید نمی‌کند.

راه‌حل:

- افزودن `ImageProvider=None`
- انتشار وردپرس بدون تصویر
- ارسال تلگرام text-only
- skip کردن اینستاگرام بدون تصویر

#### کوتاه‌بودن مقاله

مدل `llama-3.3-70b-versatile` خروجی کوتاه و ساده می‌ساخت.

راه‌حل:

- prompt اختصاصی Groq
- انتخاب `openai/gpt-oss-120b` از طریق API خود Groq
- quality gate برای حداقل طول مقاله
- provider-specific prompt paths

#### خطای JSON

خطای جدید:

```text
json_validate_failed
failed_generation: I’m sorry, but I can’t fulfill this request.
```

تحلیل:

- گاهی مدل به‌جای JSON متن refusal تولید می‌کرد.
- گاهی علت refusal نبود منابع واجد شرایط بعد از ۱ ژانویه ۲۰۲۶ بود.
- fallback کورکورانه خطرناک بود چون ممکن بود با منابع قدیمی مقاله تولید کند.

راه‌حل:

- استفاده از Structured Outputs با `json_schema` و `strict: true`
- fallback فقط برای خرابی JSON واقعی
- تشخیص refusal معنایی و توقف با پیام واضح
- دقیق‌تر کردن prompt پژوهش برای منابع بعد از ۱ ژانویه ۲۰۲۶

خروجی این مرحله:

- `GroqLlmProvider`
- `GroqApiClient`
- `GroqApiException`
- تنظیمات fallback و structured output
- prompt اختصاصی Groq

### مرحله ۹: دسته‌بندی و SEO وردپرس

در وردپرس مشخص شد مقاله در دسته‌بندی درست قرار نمی‌گیرد. بررسی REST API نشان داد:

- دسته `1` برابر «دسته‌بندی نشده» بود.
- دسته `50` برابر «مجله» بود.
- فایل env محلی مقدار `WORDPRESS_CATEGORY_ID=1` داشت و روی `appsettings` غلبه می‌کرد.

راه‌حل:

- اصلاح env محلی به `50`
- اضافه کردن `RequireCategoryId`
- validate کردن دسته پیش از تولید مقاله
- بررسی پاسخ WordPress بعد از publish تا مطمئن شویم دسته واقعاً روی post ثبت شده است.
- مستندسازی روش پیدا کردن `tag_ID` از پنل WordPress

خروجی این مرحله:

- اعتبارسنجی دسته‌بندی وردپرس
- جلوگیری از انتشار بی‌دسته یا دسته اشتباه
- مستندات SEO و CategoryId

### مرحله ۱۰: Docker و استقرار سرور

برای اجرای production روی سرور لینوکسی، پروژه Dockerized شد.

اجزای اضافه‌شده:

- `Dockerfile`
- `compose.yaml`
- `.dockerignore`
- `.env.example`
- راهنمای Docker deployment
- راهنمای فارسی قدم‌به‌قدم انتقال به سرور

تصمیم مهم این بود که promptها به‌صورت volume read-only mount شوند تا روی سرور قابل
ویرایش باشند، بدون rebuild image.

خروجی این مرحله:

- اجرای Worker در container
- restart policy
- env-based configuration
- راهنمای استقرار

## تغییرات کلیدی معماری

### از اسکریپت ساده به Worker قابل زمان‌بندی

ابتدا هدف فقط اجرای یک job در زمان مشخص بود. نتیجه نهایی یک Worker است که هم
scheduler دارد و هم manual run.

### از وابستگی مستقیم OpenAI به Provider Model

وابستگی مستقیم به OpenAI حذف شد. اکنون Providerها جدا هستند و workflow اصلی فقط با
interfaceها کار می‌کند.

### از ذخیره فایل به انتشار مستقیم

از ابتدا تصمیم گرفته شد article و image روی هاست ذخیره نشوند. تصویر در حافظه تولید
می‌شود و مستقیم به WordPress Media Library می‌رود.

### از carousel به تصویر شاخص سایت

نیاز اولیه Instagram carousel بود، اما بعداً مشخص شد برای MVP و سایت، یک تصویر شاخص
وب‌سایت مهم‌تر است. بنابراین prompt تصویر و workflow ساده‌تر شدند.

### از تصویر الزامی به تصویر اختیاری

با اضافه‌شدن Groq، چون Groq تصویر تولید نمی‌کند، image optional شد. این تغییر باعث
شد کل سیستم نسبت به نبود تصویر مقاوم شود.

### از prompt مشترک به prompt اختصاصی Provider

OpenAI و Groq رفتار متفاوتی دارند. بنابراین `ProviderPromptFilePaths` اضافه شد تا
هر Provider prompt مخصوص خود را داشته باشد.

### از publish خوش‌بینانه به publish همراه validation

در ابتدا سیستم post را ارسال می‌کرد و فرض می‌کرد درست ثبت شده است. بعدها validation
برای WordPress authentication، category و خروجی مقاله اضافه شد.

## مشکلات مهم و درس‌های فنی

### OpenAI API با ChatGPT رایگان یکی نیست

درس: API billing مستقل از حساب ChatGPT است. حتی مدل ارزان‌تر هم بدون credit یا billing
کار نمی‌کند.

### REST API وردپرس به جزئیات header حساس است

درس: برای media upload باید `Content-Type` و `Content-Disposition` درست باشند.

### خطاهای WordPress permission همیشه از password نیستند

گاهی user permission، endpoint، context یا Application Password مشکل دارد. اعتبارسنجی
قبل از publish زمان عیب‌یابی را کم می‌کند.

### Telegram bot باید عضو channel باشد

توکن درست کافی نیست. bot باید به کانال اضافه شود و مجوز ارسال داشته باشد.

### Groq Compound برای کار ترکیبی research + article مناسب نبود

درس: research و writing باید جدا شوند. Compound برای پژوهش کوتاه مناسب‌تر است.

### مدل‌ها prompt را یکسان نمی‌فهمند

OpenAI و Groq با prompt یکسان خروجی مشابه نمی‌دهند. نیاز به prompt اختصاصی و quality
gate وجود دارد.

### شرط تاریخ می‌تواند به عنوان مقاله نشت کند

وقتی گفتیم منابع بعد از ۲۰۲۶ باشند، مدل ۲۰۲۶ را در عنوان می‌آورد. درس: باید تفکیک
کنیم «شرط فیلتر منبع» با «موضوع یا عنوان مقاله» فرق دارد.

### بدون تاریخچه، جلوگیری کامل از تکرار ممکن نیست

prompt می‌تواند تنوع را بهتر کند، اما اگر دیتابیس یا حافظه مقالات قبلی نداریم، تضمین
عدم تکرار واقعی نداریم.

## فیچرهای اضافه‌شده

- مستندات پایه پروژه
- Worker scheduler
- اجرای دستی `run-once`
- OpenAI article generation
- OpenAI image generation
- promptهای قابل ویرایش
- WordPress publishing
- Instagram publishing
- Telegram publishing
- Fixture mode
- PowerShell scripts برای تست محلی
- LLM Provider abstraction
- Image Provider abstraction
- Groq provider
- None image provider
- Provider-specific prompts
- Groq web research
- Groq structured outputs
- Groq writer fallback
- WordPress category validation
- WordPress SEO metadata support
- Dockerfile و Compose
- راهنمای فارسی deployment

## وضعیت فعلی پروژه

در وضعیت فعلی سیستم می‌تواند:

- طبق زمان‌بندی روزانه اجرا شود.
- دستی با `run-once` تست شود.
- با OpenAI یا Groq مقاله بسازد.
- با Groq بدون تصویر کار کند.
- با OpenAI تصویر شاخص بسازد.
- مقاله را به وردپرس بفرستد.
- دسته‌بندی وردپرس را قبل و بعد از publish validate کند.
- خلاصه و لینک را به تلگرام بفرستد.
- اینستاگرام را فقط وقتی تصویر وجود دارد publish کند.
- با fixture بدون API تست شود.
- در Docker روی سرور لینوکس اجرا شود.

## نقاط ضعف فعلی

- دیتابیس وجود ندارد؛ بنابراین تاریخچه مقاله‌ها، جلوگیری از تکرار، audit و retry
  پایدار نداریم.
- تأیید انسانی قبل از انتشار وجود ندارد.
- queue یا job history وجود ندارد.
- مانیتورینگ production هنوز پایه‌ای است و به log محدود است.
- promptها هنوز نقش زیادی در کیفیت دارند و باید با داده واقعی بهینه شوند.
- Instagram هنوز کامل‌ترین مسیر production نیست، چون به تصویر و تنظیمات Meta وابسته
  است.
- SEO متادیتا برای بعضی pluginها ممکن است به register کردن meta در وردپرس نیاز داشته
  باشد.

## پیشنهاد مسیر ارائه

برای یک پرزنت جذاب، روایت را می‌توان این‌طور ساخت:

1. **ایده اولیه:** تولید و انتشار خودکار محتوای روزانه با AI.
2. **MVP سریع:** scheduler ساده با .NET، بدون دیتابیس و پیچیدگی اضافی.
3. **اولین خروجی واقعی:** مقاله و تصویر با OpenAI.
4. **اولین برخورد با واقعیت:** billing، خطاهای WordPress و نیاز به اجرای دستی.
5. **انتشار چندکاناله:** WordPress، Instagram و Telegram.
6. **تست بدون هزینه:** fixtureها و اسکریپت‌های محلی.
7. **تغییر معماری:** Providerها و جداسازی OpenAI/Groq/Fixture.
8. **Groq و دردسرهای production:** 413، JSON failure، مقاله کوتاه، نبود تصویر.
9. **مقاوم‌سازی:** `ImageProvider=None`, structured outputs, fallback و validation.
10. **آماده‌سازی deployment:** Docker و راهنمای سرور.
11. **درس اصلی:** اتوماسیون محتوا فقط تولید متن نیست؛ reliability، تنظیمات، publish
    validation و کیفیت خروجی همان‌قدر مهم هستند.
12. **قدم بعدی:** اضافه کردن حافظه، بازبینی انسانی، queue و داشبورد.

## پیشنهاد اسلایدها

### اسلاید ۱: مسئله

تولید محتوای روزانه زمان‌بر است و انتشار در چند کانال تکراری و مستعد خطاست.

### اسلاید ۲: هدف MVP

یک Worker سبک که روزانه محتوا تولید کند و به وردپرس و شبکه‌های اجتماعی بفرستد.

### اسلاید ۳: تصمیم‌های اولیه

بدون دیتابیس، بدون distributed lock، بدون retry پیچیده؛ تمرکز روی خروجی واقعی.

### اسلاید ۴: معماری فعلی

Scheduler -> Content Generator -> LLM Provider -> Image Provider -> Publishers.

### اسلاید ۵: Providerها

OpenAI، Groq، Fixture و None. دلیل: کاهش وابستگی و امکان تست.

### اسلاید ۶: انتشار

WordPress به‌عنوان منبع اصلی، Telegram برای بازنشر، Instagram فقط با تصویر.

### اسلاید ۷: مشکلات واقعی

OpenAI billing، WordPress REST errors، Telegram permissions، Groq 413، JSON failures.

### اسلاید ۸: راه‌حل‌های reliability

validation، fallback، fixture، structured outputs، category check، text-only mode.

### اسلاید ۹: Docker و production

اجرای Worker روی سرور لینوکس با Compose و تنظیمات امن در `.env`.

### اسلاید ۱۰: مسیر بعدی

Database، job history، duplicate detection، human approval، dashboard و analytics.

## مراحل پیشنهادی بعدی پروژه

### ۱. افزودن دیتابیس سبک

برای ثبت:

- هر اجرای job
- prompt استفاده‌شده
- منابع انتخاب‌شده
- عنوان مقاله
- لینک وردپرس
- وضعیت انتشار در هر کانال
- خطاها و retryها

گزینه مناسب: PostgreSQL یا SQLite برای شروع.

### ۲. جلوگیری از تکرار محتوا

با داشتن دیتابیس می‌توان:

- عنوان‌ها و منابع قبلی را بررسی کرد.
- قبل از publish similarity ساده گرفت.
- به prompt گفت از موضوعات قبلی فاصله بگیرد.

### ۳. مرحله بازبینی انسانی

به‌جای publish مستقیم:

- مقاله draft شود.
- لینک preview یا notification ارسال شود.
- بعد از تأیید، publish انجام شود.

### ۴. داشبورد مدیریت

یک پنل ساده برای:

- مشاهده آخرین اجراها
- retry کردن job
- تغییر prompt
- فعال/غیرفعال کردن مقصدها
- دیدن خطاها

### ۵. بهبود SEO

- تولید slug مناسب
- tagها
- schema markup
- ثبت دقیق focus keyword در plugin موردنظر وردپرس

### ۶. بهبود image workflow

- نگهداری چند template تصویر
- بررسی کیفیت تصویر
- تولید alt text جداگانه
- امکان انتخاب دستی تصویر پیش از انتشار

### ۷. مانیتورینگ production

- health check
- alert تلگرام برای خطا
- log rotation
- گزارش روزانه وضعیت job

## جمع‌بندی

این پروژه از یک ایده ساده به یک MVP واقعی رسیده است که بخش زیادی از زنجیره تولید و
انتشار محتوا را انجام می‌دهد. مهم‌ترین ارزش فنی آن فقط اتصال به AI نیست؛ بلکه
طراحی تدریجی یک workflow قابل تست، قابل تنظیم و قابل استقرار است. مسیر بعدی باید
روی حافظه، کنترل کیفیت، جلوگیری از تکرار و تجربه مدیریت انسانی تمرکز کند.
