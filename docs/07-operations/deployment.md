# استقرار و عملیات

این سند وضعیت واقعی ابزارهای استقرار ریپو را تحلیل می‌کند: Dockerfile موجود، docker-compose،
متغیرهای محیطی و نکات انتشار. نکته کلیدی: زنجیره فعلی build داکر و دیتابیس compose **هم‌خوان با کد نیست**
و این سند آن را صریح مستند می‌کند تا پیش از انتشار اصلاح شود.

## Dockerfile

تنها Dockerfile ریپو در `Presentation/Dockerfile` است (پوشه `MainApi/` وجود ندارد):

| مرحله | ایمیج | اقدامات |
|---|---|---|
| `build` | `mcr.microsoft.com/dotnet/sdk:9.0` | کپی `MainApi/MainApi.csproj`، restore، سپس publish با `-c Release` و `/p:PublishTrimmed=true` و `/p:PublishSingleFile=true` |
| `final` | `mcr.microsoft.com/dotnet/aspnet:9.0-alpine` | کپی خروجی به `/app`، ساخت کاربر غیر root با `appuser` (uid 1000)، `EXPOSE 8080`، `HEALTHCHECK` با `wget http://localhost:8080/health` هر ۳۰ ثانیه، `ENTRYPOINT ["dotnet", "MainApi.dll"]` |

**زنجیره فعلی شکسته است** و باید پیش از انتشار اصلاح شود:

1. `docker-compose.yml` به `dockerfile: MainApi/Dockerfile` ارجاع می‌دهد؛ چنین فایلی در ریپو نیست
   (Dockerfile واقعی `Presentation/Dockerfile` است).
2. خود Dockerfile هم به `MainApi/MainApi.csproj` ارجاع می‌دهد که وجود ندارد؛ پروژه اجرایی واقعی
   `Presentation/Presentation.csproj` است.
3. آرگومان `BUILD_CONFIGURATION` که compose پاس می‌دهد در Dockerfile استفاده نمی‌شود (`-c Release` هاردکد است).
4. `HEALTHCHECK` مسیر `/health` را صدا می‌زند، اما Endpointهای سلامت تعریف‌شده `/health/live` و
   `/health/ready` و `/health/details` هستند (see [cross-cutting.md](../02-architecture/cross-cutting.md)).

توجه: `.dockerignore` پوشه‌های `Tests/`، `otp.json`، `logs/` و `migrate.sql` را مستثنا نمی‌کند
و این فایل‌ها به build context ارسال می‌شوند.

## تحلیل docker-compose.yml

فایل `docker-compose.yml` ریشه سه سرویس تعریف می‌کند:

| سرویس | تصویر | نقش |
|---|---|---|
| `db` | `mcr.microsoft.com/mssql/server:2022-latest` | SQL Server 2022 با `MSSQL_PID=Developer`، پورت ۱۴۳۳، volume `mssql-data` و healthcheck با `sqlcmd SELECT 1` (هر ۱۰ ثانیه، ۳۰ بار) |
| `db-init` | `mcr.microsoft.com/mssql-tools` | پس از `sleep 20` دیتابیس `${DB_NAME}` را می‌سازد، لاگین/کاربر `${DB_USER}` می‌سازد و به نقش `db_owner` اضافه می‌کند |
| `api` | build از context ریشه | پورت ۸۰۸۰، `ASPNETCORE_URLS=http://+:8080`، `ASPNETCORE_ENVIRONMENT=Development` و `ConnectionStrings__DefaultConnection` به سرویس `db` |

**دو ناهماهنگی جدی:**

- **پروایدر دیتابیس:** کد با Npgsql به PostgreSQL وصل می‌شود
  (`options.UseNpgsql(...)` در `InfrastructureServiceExtensions.cs`)؛ دیتابیس SQL Server این compose
  هرگز به API سرویس نمی‌دهد. برای اجرای واقعی به PostgreSQL نیاز است (see [setup-and-run.md](../01-getting-started/setup-and-run.md)).
- **build سرویس api:** با نبود `MainApi/Dockerfile` (بند بالا) حتی build هم انجام نمی‌شود.

فایل `.env` متغیرهای compose (مثل `SA_PASSWORD`) نیز در ریپو موجود نیست و باید محلی ساخته شود.
هیچ فایل compose دیگری (override یا production) وجود ندارد.

## متغیرهای محیطی

| متغیر | منبع مصرف | نقش |
|---|---|---|
| `SA_PASSWORD`، `DB_NAME`، `DB_USER`، `DB_USER_PASSWORD` | فقط `docker-compose.yml` | راه‌اندازی SQL Server و ساخت دیتابیس/کاربر |
| `ASPNETCORE_URLS`، `ASPNETCORE_ENVIRONMENT` | compose و `launchSettings.json` | آدرس گوش‌دادن و محیط |
| `ConnectionStrings__DefaultConnection` | compose → کد (قرارداد `__`) | رشته اتصال زمان اجرا |
| `ASPNETCORE_ENVIRONMENT` (خواندن صریح) | `InfrastructureServiceExtensions.cs` و `DBContextFactory.cs` | غیرفعال‌کردن Retry در Development و انتخاب پیکربندی design-time |

تنها متغیری که کد با `Environment.GetEnvironmentVariable` می‌خواند همان `ASPNETCORE_ENVIRONMENT` است؛
بقیه از سازوکار استاندارد پیکربندی می‌آیند. فهرست کامل بخش‌های تنظیمات در
[01-getting-started/configuration.md](../01-getting-started/configuration.md) است.

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
| اسرار | `appsettings.json` ردیابی‌شده مقادیر حساس دارد؛ پیش از انتشار عمومی ریپو به [security.md](security.md) مراجعه شود |

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
