[بازگشت به نقشه مستندات](../README.md)

# یکپارچه‌ها و سرویس‌های زیرساختی

این بخش لایه‌های فنی اتصال به سرویس‌های بیرونی و زیرساخت‌های اجرایی را مستند می‌کند: درگاه پرداخت،
Elasticsearch، Redis، Jobهای پس‌زمینه، داده جغرافیایی و بومی‌سازی. تمرکز هر سند بر کلاس‌ها، ثبت DI،
تنظیمات و رفتار در خطا است؛ قابلیت‌های بیزینسی مربوطه در اسناد ماژول‌های
[03-modules](../03-modules/README.md) و الگوهای مشترک در [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md) آمده‌اند و تکرار نمی‌شوند.

## اسناد این بخش

| سند | موضوع |
|---|---|
| [payment-gateways.md](payment-gateways.md) | لایه فنی درگاه: `Infrastructure/Payment`، زرین‌پال، Factory، Mock، nonce، Health Check |
| [elasticsearch.md](elasticsearch.md) | لایه فنی جست‌وجو: `Infrastructure/Search`، ایندکس‌ها، Outbox اختصاصی، کلیدشکن |
| [cache-redis.md](cache-redis.md) | Redis و کش: `Infrastructure/Cache`، قفل توزیع‌شده، idempotency، ابطال کش رویدادی |
| [background-jobs.md](background-jobs.md) | فهرست کامل Jobهای پس‌زمینه و Seederها با تریگر و کار واقعی هر یک |
| [location.md](location.md) | استان/شهر از API بیرونی (`Application/Location` + `Infrastructure/Location`) |
| [localization.md](localization.md) | فرهنگ‌ها و رشته‌های خطا (`Application/Localization` + `Infrastructure/Localization`) |

## ذخیره‌سازی فایل

سند جداگانه‌ای برای `Infrastructure/Storage` وجود ندارد؛ ذخیره‌سازی شیئی S3، پویش آنتی‌ویروس و
اعتبارسنجی امضای فایل در [03-modules/media.md](../03-modules/media.md) مستند شده و آرشیو S3 لاگ‌های
حسابرسی در [03-modules/audit.md](../03-modules/audit.md).
