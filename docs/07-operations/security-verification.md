# تأیید امنیتی، چرخش اعتبارنامه‌ها و چک‌لیست انتشار

این سند «فاز ۵» ممیزی امنیتی ([SECURITY-AUDIT-REPORT.md](../../SECURITY-AUDIT-REPORT.md)) را عملیاتی می‌کند:
ابزار تأیید خودکار، رویهٔ چرخش رازها، بازنویسی تاریخچهٔ Git، گیت‌های امنیتی استارت در Production و چک‌لیست انتشار.

> **وضعیت فعلی (خلاصه):** فازهای ۰ تا ۴ پیاده‌سازی شده‌اند و گیت‌های امنیتی استارت فعال‌اند. تنها اقدام باقی‌ماندهٔ
> **دستی**، چرخش اعتبارنامه‌های افشاشده (SEC-01) و بازنویسی تاریخچهٔ Git است؛ تا آن زمان اسکن تاریخچه در CI
> به‌صورت هفتگی قرمز می‌ماند. جزئیات در بخش [وضعیت جاری](#وضعیت-جاری-تأیید).

## ۱) تأیید خودکار در یک فرمان

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/verify-security.ps1
```

> اگر PowerShell 7 نصب باشد، همان دستور با `pwsh -NoProfile -File ...` هم اجرا می‌شود؛
> همهٔ اسکریپت‌های این پوشه با Windows PowerShell 5.1 هم سازگارند.

این اسکریپت به‌ترتیب اجرا می‌کند:

| # | بررسی | ابزار | نتیجهٔ شکست |
|---|---|---|---|
| ۱ | `.env` و فایل‌های محیطی ردیابی نشده باشند | `git ls-files` | FAIL |
| ۲ | `Presentation/appsettings.json` هیچ راز زنده‌ای نداشته باشد | تحلیل JSONC | FAIL |
| ۳ | Build با هشدار به‌عنوان خطا | `dotnet build -warnaserror` | FAIL |
| ۴ | نگهبان‌های امنیتی فاز ۶ | `dotnet test --filter "Tests.Security.*"` | FAIL |
| ۵ | کل تست‌ها | `dotnet test` | FAIL |
| ۶ | بستهٔ آسیب‌پذیر NuGet | `dotnet list package --vulnerable --include-transitive` | FAIL |
| ۷ | اسرار در فایل‌های کاری | `gitleaks dir` | FAIL |
| ۸ | اسرار در تاریخچهٔ Git | `gitleaks git --log-opts=--all` | PENDING تا بازنویسی تاریخچه |

گزینه‌ها:

- `-SkipTests` برای بررسی سریع اسرار و build.
- `-FailOnHistoricalSecrets` برای **گیت انتشار**: یافته‌های تاریخچه را FAIL می‌کند (بعد از بازنویسی تاریخچه باید پاس شود).
- `-NoDownload` برای محیط بدون شبکه (اگر gitleaks نصب نباشد، مراحل اسکن SKIPPED می‌شوند).

نسخهٔ gitleaks پین‌شده (`8.30.1`) با SHA-256 رسمی در اسکریپت و در CI بررسی می‌شود؛ فایل اجرایی در صورت نبود
به‌صورت خودکار در `%LOCALAPPDATA%\gitleaks\v8.30.1` دانلود و اعتبارسنجی می‌شود.

### وضعیت جاری تأیید

| بررسی | وضعیت |
|---|---|
| Build با `-warnaserror` | PASS (۰ خطا، ۰ هشدار) |
| نگهبان‌های امنیتی فاز ۶ | PASS (۳۱ تست مجموعهٔ `Tests.Security`) |
| کل تست‌ها | PASS — ۱۱٬۵۹۹ تست (۱۰٬۰۶۸ پاس + ۱٬۵۳۱ اسکیپ دیتابیسی بدون Docker؛ در CI با Docker همه اجرا می‌شوند) |
| بسته‌های آسیب‌پذیر | PASS (هیچ بستهٔ آسیب‌پذیری در ۷ گزارش پروژه) |
| `gitleaks dir` (کد فعلی) | PASS (۰ یافته در فایل‌های قابل commit) |
| `gitleaks git` (تاریخچه) | **PASS — ۰ یافته** پس از بازنویسی تاریخچه (۲۰۲۶-۱۰-۲۰؛ پیش از آن ۱۵۰ یافته) |

### وضعیت چرخش اعتبارنامه‌ها (۲۰۲۶-۱۰-۲۰)

| راز | وضعیت | توضیح |
|---|---|---|
| `Jwt__Key` | ✅ چرخش شد | کلید ۴۸ بایتی تصادفی جدید (Base64) در `.env`؛ مقادیر قدیمی در تاریخچه حذف شدند |
| `Auth__OtpPepper` | ✅ چرخش شد | مقدار تصادفی جدید در `.env`؛ OTPهای صادرشدهٔ قبلی باطل شدند |
| رمز PostgreSQL / Supabase | ⏳ نیازمند داشبورد Supabase | ریست رمز دیتابیس و به‌روزرسانی `ConnectionStrings__*` |
| کلیدهای ArvanCloud S3 | ⏳ نیازمند پنل آروان | ساخت کلید جدید و ابطال کلید قبلی |
| کلید API کاوه‌نگار | ⏳ نیازمند پنل کاوه‌نگار | صدور کلید جدید و غیرفعال‌کردن قبلی |
| شمارهٔ ادمین bootstrap | ✅ خالی است | `InitialAdmin__PhoneNumbers` در appsettings خالی است |

> کلیدهای سرویس‌های بیرونی فقط از داشبورد خودشان قابل چرخش‌اند؛ مقادیر قبلی آن‌ها باید «لو رفته» فرض شوند
> و پس از چرخش در `.env`/Secret Manager جایگزین شوند. اسکن تاریخچه بدون توجه به این مقادیر پاس است.

## ۲) سه لایهٔ اجرای اسکن اسرار

| لایه | فایل | چه‌وقت اجرا می‌شود | شکست چه می‌کند |
|---|---|---|---|
| Pre-commit (بدون وابستگی) | `.githooks/pre-commit` | هر commit، پس از `scripts/security/install-git-hooks.ps1` | commit را متوقف می‌کند (gitleaks یا اسکنر جایگزین PowerShell) |
| Pre-commit (چارچوب) | `.pre-commit-config.yaml` | `pre-commit install` | مانند بالا + block فایل‌های محیطی |
| CI | `.github/workflows/ci.yml` | هر push/PR روی بازهٔ commitها؛ تاریخچهٔ کامل هفتگی | Pipeline قرمز می‌شود |

راه‌اندازی روی هر کلون:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/install-git-hooks.ps1
# یا
pip install pre-commit; pre-commit install
```

> نکته: workflow قبلی `ci.yml` یک فایل **خراب (اکسل)** بود و هیچ‌گاه اجرا نمی‌شد؛ در فاز ۵ با workflow واقعی
> جایگزین شد. از این پس lint/build/test/audit و اسکن اسرار واقعاً در CI اجرا می‌شوند.

## ۳) چرخش اعتبارنامه‌ها (SEC-01) — اقدام فوری

هر رازی که پیش از فاز ۰ در Git بود، **لو رفته فرض می‌شود**. ترتیب پیشنهادی:

| # | سرویس | اقدام | تأیید |
|---|---|---|---|
| ۱ | PostgreSQL / Supabase | Reset password در پنل دیتابیس؛ کاربر برنامه با حداقل دسترسی؛ حذف کاربر قدیمی | اتصال با رمز جدید از `.env`؛ `ConnectionStrings__DefaultConnection` به‌روزرسانی شد |
| ۲ | ArvanCloud S3 | ساخت کلید جدید (یا کاربر جدید با دسترسی محدود به باکت `ledka`) و ابطال کلید قدیمی | آپلود/دانلود یک فایل تست از پنل ادمین |
| ۳ | Kavenegar | صدور API Key جدید و غیرفعال‌کردن کلید قبلی | ارسال OTP تستی |
| ۴ | JWT | تولید کلید جدید: `openssl rand -base64 48` (حداقل ۳۲ بایت) و قراردادن در `Jwt__Key` | ورود موفق؛ توکن‌های قدیمی نامعتبر می‌شوند (کاربران باید دوباره وارد شوند) |
| ۵ | OTP Pepper | تولید مقدار جدید ≥۱۶ کاراکتر در `Auth__OtpPepper` | کدهای OTP صادرشدهٔ قبلی باطل می‌شوند (پذیرفته‌شده) |
| ۶ | شمارهٔ ادمین bootstrap | در Production لیست `InitialAdmin__PhoneNumbers` را پس از bootstrap خالی کنید | در استارت هشدار مربوط به `InitialAdmin` لاگ نشود |
| ۷ | Redis | در صورت افشا: رمز جدید + `ssl=true` | `Cache__RedisConnectionString` به‌روزرسانی شد |

اعتبارسنجی استارت (بخش ۵) اجازه نمی‌دهد کلید JWT با الگوی لو‌رفته (`Breaking\d+Bad`) در Production بالا بیاید؛
همچنین `Auth__OtpPepper` را اجباری می‌کند.

## ۴) بازنویسی تاریخچهٔ Git

**وضعیت: انجام شد (۲۰۲۶-۱۰-۲۰).** تاریخچهٔ محلی بازنویسی شد و اسکن `gitleaks git` از **۱۵۰ یافته به صفر** رسید.
پس از بازنویسی: `349` commit بازنویسی‌شده، مسیرهای `otp.json` / `Presentation/appsettings.Production.json` /
`MainApi/.env` / `MainApi/appsettings.json` / `DataAccessLayer/MechanicContext.cs` حذف شدند و مقادیر راز
(کلید JWT، کلید کاوه‌نگار، رمز رشته‌های اتصال) با `CHANGE_ME` جایگزین شدند. بکاپ پیش از بازنویسی:
`%TEMP%\mechanic-pre-rewrite-*.bundle`.

رویهٔ اجرا (برای بازتولید یا کلون جدید):

```powershell
# ۱) نصب git-filter-repo (یا استفاده از ماژول پایتون)
pip install git-filter-repo

# ۲) دیدن نقشهٔ کار (بدون تغییر)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/purge-git-history.ps1

# ۳) اجرای واقعی (بکاپ bundle به‌صورت پیش‌فرض در %TEMP% ساخته می‌شود)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/purge-git-history.ps1 -Execute
```

اسکریپت این کارها را انجام می‌دهد:

1. بررسی وجود `git filter-repo` (یا ماژول پایتون)، clean بودن working tree و ساخت بکاپ `git bundle`.
2. حذف مسیرهای کل لو‌رفته: `otp.json`، `Presentation/appsettings.Production.json`، `MainApi/.env`، `MainApi/appsettings.json`.
3. ساخت فهرست redaction از گزارش gitleaks (مقادیر راز هرگز چاپ نمی‌شوند) و اجرای `git filter-repo --replace-text`
   برای پاک‌کردن مقادیر باقی‌مانده در سایر فایل‌های تاریخی (SMS، connection string، docker-compose، appsettings).
4. بازنویسی همهٔ commitها و tagها.

پس از بازنویسی، remote دوباره وصل شد و تاریخچهٔ بازنویسی‌شده با `--force-with-lease` روی `origin/main` منتشر شد (remote اکنون همان تاریخچهٔ پاک را دارد). برای تکرار در آینده:

```powershell
git push --force-with-lease origin main
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/verify-security.ps1 -FailOnHistoricalSecrets   # باید PASS شود
```

> ⚠️ همهٔ هش‌های commit عوض می‌شوند و هر همکار باید **دوباره clone** کند؛ کلون‌های قدیمی همچنان رازها را دارند.
>
> **نکتهٔ GitHub:** پس از بازنویسی، `refs/pull/*` (و fork‌های موجود) ممکن است همچنان به commitهای قدیمی اشاره کنند و قابل حذف با push نیستند.
> تا زمانی که GitHub Support آن‌ها را purge نکند، چرخش کلیدهای بیرونی (بخش ۳) تنها راه بی‌اثرکردن مقادیر لو‌رفته است.
> همچنین job هفتگی `secret-history-scan` در CI پس از push همین تاریخچه سبز می‌شود (روی کلون محلی اکنون سبز است).

## ۵) گیت‌های امنیتی استارت در Production

`ValidateRequiredConfiguration` در `Presentation/Common/Extensions/ConfigurationValidationExtension.cs` هنگام
استارت (فقط در `Production`) این موارد را اجباری می‌کند؛ در غیر این صورت اپ با پیام صریح بالا نمی‌آید:

| گیت | تنظیم لازم | یافتهٔ مرتبط |
|---|---|---|
| کلید JWT لو‌رفته | `Jwt:Key` متفاوت از الگوی `Breaking\d+Bad` | SEC-01 |
| Pepper OTP | `Auth:OtpPepper` با حداقل ۱۶ کاراکتر | SEC-08 |
| AllowedHosts | بدون `*` (لیست دامنه‌ها) | SEC-20 |
| IP ادمین | `Security:AdminIpWhitelist` غیرخالی | SEC-05 |
| Originهای مجاز | `Security:AllowedOrigins` غیرخالی و بدون wildcard | SEC-11/SEC-12 |
| وب‌هوک پرداخت | `FeatureManagement__Payment.Callback.IpWhitelistRequired=true` | SEC-06 |
| IPهای درگاه | `Zarinpal:AllowedIps` غیرخالی | SEC-06 |
| Redis + TLS | `Cache:UseRedis=true` و `ssl=true` در رشتهٔ Redis | فاز ۰/۲ |
| رمزنگاری کش | `Cache:Encryption:KeyBase64` (۳۲ بایت) + `UseTls=true` | فاز ۰ |
| Swagger | فقط هشدار (توصیه: `Swagger__Enabled=false`) | — |

نمونهٔ کامل متغیرها در `.env.example` (بخش «Production only») آمده است.

## ۶) چک‌لیست انتشار (قبل از هر استقرار Production)

- [ ] همهٔ اعتبارنامه‌های SEC-01 چرخش کرده‌اند و مقادیر جدید فقط در Secret Manager / env هستند.
- [ ] `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/verify-security.ps1 -FailOnHistoricalSecrets` پاس می‌شود.
- [ ] `git log --all -p | Select-String "Breaking"` خروجی ندارد (تاریخچه پاک شده است).
- [ ] `AllowedHosts`، `Security__AdminIpWhitelist`، `Security__AllowedOrigins` پر هستند و هیچ wildcard ندارند.
- [ ] `FeatureManagement__Payment.Callback.IpWhitelistRequired=true` و `Zarinpal__AllowedIps` با IPهای رسمی درگاه پر است.
- [ ] `Cache__UseRedis=true` و رشتهٔ Redis/دیتابیس با `ssl=true` و **بدون** `TrustServerCertificate=true` است.
- [ ] `InitialAdmin__PhoneNumbers` خالی است (bootstrap انجام شده).
- [ ] `Swagger__Enabled=false` و `PaymentGateway__EnableMockGateway=false`.
- [ ] `dotnet list package --vulnerable` بدون یافته است (job `dependency-audit` سبز است).
- [ ] بکاپ دیتابیس و Redis (شامل کلیدهای DataProtection) گرفته شده است.
- [ ] اجرای واقعی اپ با `.env`/env تأیید شده و `/health/live` و `/health/ready` پاسخ ۲۰۰ می‌دهند.
- [ ] تمام تست‌ها در CI (با Docker، شامل تست‌های Testcontainers) سبز هستند.

## ۷) نگهداری مستمر

| مورد | تناوب | ابزار |
|---|---|---|
| اسکن کامل تاریخچهٔ Git | هفتگی | job `secret-history-scan` در CI |
| به‌روزرسانی بسته‌ها | هفتگی | `.github/dependabot.yml` (NuGet، Actions، Docker) |
| اسکن اسرار در commit | هر commit | `.githooks/pre-commit` + `.pre-commit-config.yaml` |
| بازبینی امنیتی دوره‌ای (OWASP Top 10) | هر ۶ ماه | `SECURITY-AUDIT-REPORT.md` به‌عنوان baseline |
| چرخش کلید JWT / Pepper / کلیدهای سرویس | هر ۹۰–۱۸۰ روز | بخش ۳ همین سند |

## اسناد مرتبط

- گزارش ممیزی و وضعیت فازها: [SECURITY-AUDIT-REPORT.md](../../SECURITY-AUDIT-REPORT.md)
- تنظیمات و متغیرهای محیطی: [01-getting-started/configuration.md](../01-getting-started/configuration.md)
- امنیت عملیاتی و ماژول امنیت: [security.md](security.md)
- انتشار و Docker: [deployment.md](deployment.md)
