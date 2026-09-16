# راه‌اندازی و اجرا

این سند مسیر عملی اجرای پروژه را روی ماشین توسعه می‌دهد: پیش‌نیازها، آماده‌سازی PostgreSQL،
اعمال مایگریشن‌ها با dotnet-ef و اجرای API. نکته مهم: کد فعلی روی PostgreSQL اجرا می‌شود،
در حالی که `docker-compose.yml` ریپو یک SQL Server بالا می‌آورد؛ این ناهماهنگی در همین سند و
[07-operations/deployment.md](../07-operations/deployment.md) به‌صورت صریح توضیح داده شده است.

## پیش‌نیازها

| پیش‌نیاز | نسخه / منبع | توضیح |
|---|---|---|
| .NET SDK | `9.0.314` طبق `global.json` (با `rollForward: latestFeature`) | بدون نصب این نسخه، build با خطای SDK مواجه می‌شود |
| PostgreSQL | نسخه‌ای سازگار با Npgsql 9 | سرور اجرا؛ می‌تواند لوکال، Docker یا سرویس ابری باشد |
| Docker | هر engine سازگار با API داکر | برای تست‌های یکپارچه (Testcontainers) لازم است؛ جزئیات در [06-testing/testing-strategy.md](../06-testing/testing-strategy.md) |
| dotnet-ef | `9.0.17` طبق manifest محلی (`dotnet-tools.json`) | با `dotnet tool restore` نصب می‌شود |

> **نکته مهم — ناهماهنگی docker-compose با کد:** سرویس `db` در `docker-compose.yml` تصویر
> `mcr.microsoft.com/mssql/server:2022-latest` را اجرا می‌کند، اما پروایدر دیتابیس در کد
> Npgsql/PostgreSQL است (`Infrastructure/Common/DependencyInjection/InfrastructureServiceExtensions.cs`
> با `options.UseNpgsql(...)`). بنابراین دیتابیس SQL Server این compose در وضعیت فعلی به API سرویس نمی‌دهد.
> به‌علاوه سرویس `api` به `MainApi/Dockerfile` ارجاع می‌دهد که در ریپو وجود ندارد. تحلیل کامل در
> [deployment.md](../07-operations/deployment.md) آمده است.

## گام ۱: دریافت کد و Build

```bash
dotnet restore
dotnet build API.sln
```

پروژه اجرایی `Presentation` است؛ نقطه ورود `Presentation/Program.cs`.

## گام ۲: آماده‌سازی PostgreSQL و رشته‌های اتصال

تنها `Presentation/appsettings.json` در git ردیابی می‌شود؛ فایل‌های
`appsettings.Development.json` و `appsettings.Production.json` در `.gitignore` هستند
و روی هر ماشین باید محلی ساخته شوند. رشته‌های اتصال لازم:

| کلید | استفاده |
|---|---|
| `ConnectionStrings:DefaultConnection` | اتصال زمان اجرا (`AddPersistence`) |
| `ConnectionStrings:MigrationConnection` | فقط برای `DBContextFactory` هنگام دستورات dotnet-ef |
| `ConnectionStrings:Redis` | Redis کش (وقتی `Cache:UseRedis=true`؛ در غیر این صورت fallback حافظه‌ای فعال می‌شود) |

می‌توان رشته‌های اتصال را به‌جای فایل، با متغیر محیطی (قرارداد `__` در .NET) داد، مثلاً
`ConnectionStrings__DefaultConnection`. هنگام استارت، `ValidateRequiredConfiguration` در
`Presentation/Common/Extensions/ConfigurationValidationExtension.cs` وجود
`DefaultConnection` و کلید JWT (حداقل ۳۲ بایت) و بخش `Storage` را الزامی می‌کند؛ فهرست کامل
بخش‌های تنظیمات در [configuration.md](configuration.md) آمده است.

## گام ۳: اعمال مایگریشن‌ها

اپلیکیشن هنگام استارت **مایگریشن خودکار ندارد** (هیچ فراخوانی `Migrate()`/`EnsureCreated()` در کد اجرایی نیست).
مایگریشن‌ها با dotnet-ef و از طریق `DBContextFactory`
(`Infrastructure/Persistence/Context/DBContextFactory.cs`، که رشته `MigrationConnection` را می‌خواند) اعمال می‌شوند:

```bash
dotnet tool restore

# اعمال مایگریشن‌ها روی دیتابیس
dotnet ef database update --project Infrastructure --startup-project Presentation

# افزودن مایگریشن جدید
dotnet ef migrations add <MigrationName> --project Infrastructure
```

تنها مایگریشن موجود در کد `20260908041205_1` در `Infrastructure/Persistence/Migrations/` است.
دو فایل SQL دیگر در ریپو برای اعمال اسکیمای به‌روز قابل اتکا نیستند:

| فایل | وضعیت |
|---|---|
| `migrate.sql` (ریشه) | اسکریپت تولیدشده EF برای مایگریشن قدیمی `20260603054724_1` با ۴۲ جدول؛ نسبت به مایگریشن فعلی `20260908041205_1` **قدیمی** است |
| `Presentation/migration.txt` | اسکریپت بازنشانی مخرب (`DROP SCHEMA public CASCADE; CREATE SCHEMA public;`)؛ فقط برای از نو ساختن اسکیمای توسعه |

## گام ۴: اجرای API

```bash
dotnet run --project Presentation
```

طبق `Presentation/Properties/launchSettings.json`، پروفایل‌ها روی HTTP با پورت `44317` و
HTTPS با پورت `44318` تنظیم‌اند. برای بررسی سریع سلامت:

| مسیر | توضیح |
|---|---|
| `GET /` | بدون احراز هویت؛ `{ status: "Healthy", timestamp: ... }` برمی‌گرداند |
| `GET /health/live` و `GET /health/ready` | Health Checkهای liveness/readiness (فهرست چک‌ها در [cross-cutting.md](../02-architecture/cross-cutting.md)) |
| `GET /swagger` | UI سوگر، فقط وقتی `Swagger:Enabled=true` باشد (در Production پیش‌فرض `false`) |

## وابستگی‌های اختیاری

Redis و Elasticsearch با فلگ خاموش/روشن می‌شوند و نبودشان اجرا را متوقف نمی‌کند:
`Cache:UseRedis` برای کش/قفل توزیع‌شده/idempotency (fallback حافظه‌ای) و
`Elasticsearch:IsEnabled` برای جست‌وجو (fallbackهای `NoOp*` در `Infrastructure/Search`).
پیامک OTP به API کاوه‌نگار (`Kavenegar:ApiKey`) و پرداخت به زرین‌پال (`Zarinpal` گزینه Sandbox دارد) وصل می‌شود.

## docker-compose در وضعیت فعلی

فایل `docker-compose.yml` سه سرویس تعریف می‌کند: `db` (SQL Server 2022 با healthcheck و volume
`mssql-data`)، `db-init` (پس از ۲۰ ثانیه دیتابیس و لاگین `DB_USER` می‌سازد) و `api`
(پورت ۸۰۸۰، `ASPNETCORE_ENVIRONMENT=Development`). متغیرهای محیطی آن (`SA_PASSWORD`،
`DB_NAME`، `DB_USER`، `DB_USER_PASSWORD`) از فایل `.env` خوانده می‌شوند که در ریپو موجود نیست.
به دو دلیل ذکرشده در ابتدای این سند (پروایدر دیتابیس و مسیر Dockerfile)، این فایل در حالت فعلی
راه‌اندازی یک‌جای پروژه را ممکن نمی‌کند؛ تحلیل کامل و نکات انتشار در
[07-operations/deployment.md](../07-operations/deployment.md) است.
