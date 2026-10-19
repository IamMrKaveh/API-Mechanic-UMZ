# استقرار و عملیات

این سند وضعیت واقعی ابزارهای استقرار ریپو را تحلیل می‌کند: Dockerfile موجود، docker-compose،
متغیرهای محیطی و نکات انتشار. زنجیرهٔ build داکر در فاز ۴ ممیزی امنیتی اصلاح شد؛ تنها ناهماهنگی
باقی‌مانده، پروایدر دیتابیس compose (SQL Server) در برابر کد (PostgreSQL) است.

## Dockerfile

تنها Dockerfile ریپو در `Presentation/Dockerfile` است:

| مرحله | ایمیج | اقدامات |
|---|---|---|
| `build` | `mcr.microsoft.com/dotnet/sdk:10.0` | کپی کل context فیلترشده (`.dockerignore`)، `dotnet restore Presentation/Presentation.csproj`، سپس publish با `-c ${BUILD_CONFIGURATION}` (پیش‌فرض Release) و `/p:PublishTrimmed=true` / `/p:PublishSingleFile=true` |
| `final` | `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` | کپی خروجی به `/app`، کاربر غیر root `appuser` (uid 1000)، `EXPOSE 8080`، `HEALTHCHECK` روی `http://localhost:8080/health/live` هر ۳۰ ثانیه، `ENTRYPOINT ["dotnet", "Presentation.dll"]` |

مسائل زنجیرهٔ قبلی که در فاز ۴ رفع شدند:

1. `docker-compose.yml` به `dockerfile: MainApi/Dockerfile` ارجاع می‌داد که وجود ندارد → اکنون `Presentation/Dockerfile`.
2. Dockerfile به `MainApi/MainApi.csproj` ارجاع می‌داد → اکنون restore چندپروژه‌ای روی `Presentation/Presentation.csproj`.
3. آرگومان `BUILD_CONFIGURATION` استفاده نمی‌شد → اکنون به‌عنوان `ARG` واقعی به publish داده می‌شود.
4. `HEALTHCHECK` مسیر `/health` را صدا می‌زد → اکنون `/health/live` (Endpointهای سلامت: `/health/live`، `/health/ready`، `/health/details` — see [cross-cutting.md](../02-architecture/cross-cutting.md)).

`.dockerignore` اکنون `appsettings.Development.json`، `appsettings.Production.json`، `logs/`، `migrate.sql` و
فایل‌های compose/Dockerfile را از context حذف می‌کند تا تنظیمات محیطی داخل ایمیج پخته نشوند.

## تحلیل docker-compose.yml

فایل `docker-compose.yml` ریشه سه سرویس تعریف می‌کند (استک توسعهٔ محلی):

| سرویس | تصویر | نقش |
|---|---|---|
| `db` | `mcr.microsoft.com/mssql/server:2022-latest` | SQL Server 2022 با `MSSQL_PID=Developer`، پورت ۱۴۳۳، volume `mssql-data` و healthcheck با `sqlcmd -C` و رمز از `SQLCMDPASSWORD` (رمز در process list دیده نمی‌شود) |
| `db-init` | `mcr.microsoft.com/mssql-tools` | پس از سالم‌شدن `db`، دیتابیس `${DB_NAME}`، لاگین/کاربر `${DB_USER}` و نقش `db_owner` را می‌سازد |
| `api` | build از context ریشه | پورت ۸۰۸۰، `ASPNETCORE_URLS=http://+:8080`، `ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Production}` و `ConnectionStrings__DefaultConnection` به `db` با `Encrypt=True` |

نکات:

- **پروایدر دیتابیس:** کد با Npgsql به PostgreSQL وصل می‌شود (`options.UseNpgsql(...)` در `InfrastructureServiceExtensions.cs`)؛
  دیتابیس SQL Server این compose به API سرویس نمی‌دهد. برای اجرای واقعی به PostgreSQL نیاز است
  (see [setup-and-run.md](../01-getting-started/setup-and-run.md)).
- **TLS دیتابیس:** `Encrypt=True` روشن است؛ `TrustServerCertificate=True` فقط برای گواهی self-signed کانتینر محلی
  است و در استقرار واقعی باید با CA معتبر و `TrustServerCertificate=False` جایگزین شود.
- **محیط پیش‌فرض Production است** تا صفحه‌های خطای Development در کانتینر سرو نشوند؛ برای استک محلی
  در `.env` مقدار `ASPNETCORE_ENVIRONMENT=Development` را فعال کنید (اعتبارسنجی Production به Redis با TLS نیاز دارد).
- همهٔ رازها از `.env` (gitignored، قالب `.env.example`) می‌آیند؛ هیچ فایل compose دیگری وجود ندارد.

## متغیرهای محیطی

| متغیر | منبع مصرف | نقش |
|---|---|---|
| `SA_PASSWORD`، `DB_NAME`، `DB_USER`، `DB_USER_PASSWORD` | فقط `docker-compose.yml` | راه‌اندازی SQL Server و ساخت دیتابیس/کاربر |
| `ASPNETCORE_URLS`، `ASPNETCORE_ENVIRONMENT` | compose و `launchSettings.json` | آدرس گوش‌دادن و محیط |
| `ConnectionStrings__DefaultConnection` | compose → کد (قرارداد `__`) | رشته اتصال زمان اجرا |
| `Jwt__Key`، `Auth__OtpPepper`، `Storage__*`، `Kavenegar__ApiKey`، `ConnectionStrings__*` | `.env` → `DotEnvLoader` → کانفیگ | رازها (فاز ۰) |
| `FeatureManagement__Payment.Callback.*`، `Zarinpal__AllowedIps__*` | `.env`/env → کانفیگ | سخت‌سازی وب‌هوک پرداخت (فاز ۲) |
| `ASPNETCORE_ENVIRONMENT` (خواندن صریح) | `InfrastructureServiceExtensions.cs` و `DBContextFactory.cs` | غیرفعال‌کردن Retry در Development و انتخاب پیکربندی design-time |

تنها متغیری که کد با `Environment.GetEnvironmentVariable` می‌خواند همان `ASPNETCORE_ENVIRONMENT` است؛
بقیه از سازوکار استاندارد پیکربندی می‌آیند. فهرست کامل بخش‌های تنظیمات در
[01-getting-started/configuration.md](../01-getting-started/configuration.md) و فهرست الزامات Production در
[security-verification.md](security-verification.md) است.

## مایگریشن‌ها در عملیات

- اپلیکیشن هنگام استارت مایگریشن نمی‌کند (`Database.Migrate` در کد اجرایی فراخوانی نمی‌شود)؛
  اعمال مایگریشن باید مرحله جداگانه استقرار باشد: `dotnet ef database update --project Infrastructure --startup-project Presentation`
  (نیازمند `ConnectionStrings:MigrationConnection` — see [setup-and-run.md](../01-getting-started/setup-and-run.md)).
- مایگریشن فعلی: `20260908041205_1` در `Infrastructure/Persistence/Migrations/`.
- اسکریپت `migrate.sql` ریشه مربوط به مایگریشن قدیمی `20260603054724_1` است و به‌روز نیست؛
  برای تولید اسکریپت به‌روز می‌توان `dotnet ef migrations script` را روی نسخه فعلی اجرا کرد.
- `Presentation/migration.txt` اسکریپت بازنشانی کامل اسکیما (`DROP SCHEMA public CASCADE`) است و در محیط مشترک هرگز اجرا نشود.

## نکات انتشار (Production)

بر پایه `appsettings.Production.json`، اعتبارسنجی استارت (`ValidateRequiredConfiguration`) و رفتار Pipeline:

| نکته | توضیح |
|---|---|
| Swagger | با `Swagger:Enabled=false` خاموش می‌شود |
| امنیت Callback پرداخت | فلگ `Payment.Callback.SignatureRequired` در Production روشن است؛ `WebhookIpWhitelistMiddleware` هم IP درگاه‌ها را محدود می‌کند |
| Redis اجباری و TLS | در Production اعتبارسنجی، روشن‌بودن `Cache:UseRedis` و وجود `ssl=true` در رشته Redis را الزامی می‌کند |
| آرشیو حسابرسی | `Storage:AuditArchive:Provider=S3` (به‌جای File System توسعه) |
| لاگ | Sinkهای فعال: Console (Warning+)، `Logs/log-YYYYMMDD.json` (Warning+، ۱۴ فایل) و `Logs/errors/` (Error+، ۹۰ فایل) — پیکربندی در کد است، نه بخش `Serilog` فایل‌های تنظیمات |
| رؤیت‌پذیری | OpenTelemetry به `OpenTelemetry:OtlpEndpoint` ارسال می‌کند |
| سرویس‌های پس‌زمینه | همه Jobها و Seederها درون فرایند API اجرا می‌شوند (نه سرویس جدا)؛ در استقرار چندنمونه‌ای، اجرای هم‌زمانشان با قفل Redis کلید `jobs:*` ایمن شده است |
| اسرار | `appsettings.json` ردیابی‌شده فقط placeholder خالی دارد؛ مقدار واقعی از `.env`/Secret Manager می‌آید و گیت‌های استارت Production در [security-verification.md](security-verification.md) فهرست شده‌اند |

## فایل‌های متفرقه ریشه

چند فایل غیر NET. در ریشه ریپو هست که بخشی از بیلد یا اجرای سرویس نیستند؛ برای کامل‌بودن نقشه ریپو مستند می‌شوند:

| فایل | چیست | وضعیت |
|---|---|---|
| `extract.py` | اسکریپت پایتون مستقل (۳۷۴ خط) برای استخراج فایل‌های کد از خروجی متنی LLM — بلوک‌های `@@FILE ... @@END` را می‌خواند و روی دیسک می‌نویسد؛ ابزار کمکی زمان scaffold اولیه پروژه | در سولوشن NET. یا csproj/sln مصرف نمی‌شود؛ اگر دیگر لازم نیست قابل حذف است |
| `extract.txt` | فایل ورودی پیش‌فرض `extract.py` (هر دو در یک مسیر جای‌گذاری شده‌اند) | فعلاً خالی (فقط یک خط جدید) و بدون محتوا |
| `using` و `Domain.Product.ValueObjects;` | دو فایل صفر بایتی؛ نام‌ها تکه‌های جدا‌شده جمله `using Domain.Product.ValueObjects;` در C# هستند — به‌نظر می‌رسد حاصل paste اشتباه این جمله در پوسته بوده که به‌جای کامپایل به‌صورت ریدایرکت دو فایل ساخته | artifact تصادفی؛ در کد مصرف نمی‌شود و قابل حذف است |
| `.config/dotnet-tools.json` | Manifest رسمی ابزارهای NET. — نسخه `dotnet-ef` را روی `9.0.17` قفل می‌کند | معتبر و مرتبط؛ با `dotnet tool restore` مصرف می‌شود (دستورهای ef در [setup-and-run.md](../01-getting-started/setup-and-run.md)) |

## اسناد مرتبط

- اجرای محیط توسعه: [01-getting-started/setup-and-run.md](../01-getting-started/setup-and-run.md)
- Jobها، Health Check و Outbox: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
- اسکن اسرار و pre-commit: [security.md](security.md)
- تأیید امنیتی، چرخش رازها و چک‌لیست انتشار: [security-verification.md](security-verification.md)
