# امنیت عملیاتی

این سند فقط امور عملیاتی امنیت را پوشش می‌دهد: زیرساخت DataProtection، ماژول `Infrastructure/Security`،
گیت‌های امنیتی استارت، ابزارهای اسکن اسرار (gitleaks، hookهای Git و CI) و نگهداری کلیدها. جریان‌های احراز هویت،
OTP و سشن‌های کاربر موضوع سند آینده `03-modules/identity/auth-security.md` هستند و اینجا فقط ارجاع می‌شوند.
رویهٔ چرخش رازها، بازنویسی تاریخچه و چک‌لیست انتشار در [security-verification.md](security-verification.md) است.

## DataProtection

- پیاده‌سازی کلیدها در `Infrastructure/DataProtection/Repositories/ResilientRedisXmlRepository.cs` است:
  یک `IXmlRepository` سفارشی که کلیدهای ASP.NET Core Data Protection را به‌صورت XML در Redis
  (پیشوند کلید `DataProtection`، انقضای ۹۰ روز) نگه می‌دارد.
- «Resilient» بودن یعنی هنگام از کارافتادگی Redis به‌جای پرتاب استثنا، هشدار لاگ می‌شود و لیست خالی برمی‌گردد
  تا کل پروسه نیفتد.
- اتصال در `AddDataProtectionLayer` (داخل `InfrastructureServiceExtensions.cs`) فقط وقتی
  `Cache:UseRedis=true` فعال است؛ در غیر این صورت رفتار پیش‌فرض (فایل/حافظه) می‌ماند. کلید تنظیمی
  `Security:DataProtectionPath` در appsettings وجود دارد.
- پیامد عملیاتی: در استقرار چندنمونه‌ای، روشن‌بودن Redis شرط یکسان‌بودن کلیدهای کوکی/توکن بین نمونه‌هاست
  (پیش‌نیاز Production طبق اعتبارسنجی استارت — see [01-getting-started/configuration.md](../01-getting-started/configuration.md)).

## ماژول Infrastructure/Security

ساختار واقعی پوشه `Infrastructure/Security`:

| جزء | کلاس | نقش |
|---|---|---|
| سرویس هش | `PasswordHasher` | پیاده‌سازی `IPasswordHasher` (قرارداد در `Application/Security/Contracts/`) با BCrypt.Net-Next و work factor ۱۲ |
| Rate Limit با Redis | `RateLimitService` | شمارنده‌های نرخ روی Redis |
| Rate Limit حافظه‌ای | `InMemoryRateLimitService` | جایگزین داخلی |
| Rate Limit مقاوم | `ResilientRateLimitService` | Redis با fallback خودکار به حافظه |
| موجودیت | `RateLimitEntry` + `RateLimitEntryConfiguration` | رکورد دائمی نرخ در دیتابیس (EF) |
| تنظیمات | `SecuritySettings` (سکشن `Security`: `AdminIpWhitelist`، `AllowedOrigins`)، `GoogleAuthSettings` | Bind از افزونه‌های Presentation |

تولید توکن JWT در ماژول Auth و نه Security است (`Infrastructure/Auth/Services/JwtTokenGenerator.cs`).

## Middlewareهای مرتبط با امنیت در Pipeline

| Middleware | نقش عملیاتی |
|---|---|
| `SecurityHeadersMiddleware` | ست‌کردن CSP (با nonce، `EnableNonce=true`)، HSTS، `X-Frame-Options`، `Referrer-Policy`، `Permissions-Policy` و COOP/COEP/CORP (هر سه در appsettings فعال‌اند) طبق `SecurityHeadersOptions` |
| `UseAdminIpWhitelist` | محدودکردن مسیرهای ادمین (`/api/admin` و `/api/v{version}/admin`) به `Security:AdminIpWhitelist`؛ **fail-closed** وقتی لیست خالی است |
| `WebhookIpWhitelistMiddleware` | پذیرش Callbackهای درگاه پرداخت فقط از IPهای مجاز (`Webhook:AllowedPaths`، `Zarinpal:AllowedIps`)؛ غیرفعال‌بودن فلگ یا لیست خالی هر دو در لاگ ثبت می‌شوند |
| `SessionActivityMiddleware` | ردیابی آخرین فعالیت سشن‌های کاربر (جزئیات سشن: سند آینده `03-modules/identity/auth-security.md`) |
| `UseApplicationAntiforgery` | محافظت ضد CSRF |

## گیت‌های امنیتی استارت (Production)

`ValidateRequiredConfiguration` (در `Presentation/Common/Extensions/ConfigurationValidationExtension.cs`) هنگام
استارت این موارد را بررسی می‌کند و در Production با پیام صریح متوقف می‌شود:

1. `ConnectionStrings:DefaultConnection`، `Jwt:Key` (≥۳۲ بایت)، `Jwt:Issuer`/`Jwt:Audience`، کلیدهای Storage،
   `Kavenegar:ApiKey` و `ZarinPal:MerchantId`.
2. حضور همهٔ فلگ‌های `FeatureManagement` (فهرست `FeatureFlags.All`).
3. Redis با TLS و کلید رمزنگاری کش (۰/۲).
4. `Security:AdminIpWhitelist` غیرخالی و `Security:AllowedOrigins` غیرخالی/بدون wildcard (۰۵/۱۱/۱۲).
5. `FeatureManagement__Payment.Callback.IpWhitelistRequired=true` و `Zarinpal:AllowedIps` غیرخالی (۰۶).
6. `Auth:OtpPepper` با حداقل ۱۶ کاراکتر و رد کلید JWT لو‌رفته (۰۸/۰۱).
7. `AllowedHosts` بدون `*` (۲۰).

هشدار (بدون توقف): پر بودن `InitialAdmin:PhoneNumbers` در Production و روشن بودن `Swagger:Enabled`.

## اسکن اسرار با gitleaks

`.gitleaks.toml` قواعد پیش‌فرض gitleaks (`useDefault = true`) را گسترش می‌دهد و بر پایهٔ نسخهٔ `8.30.1` است.
همهٔ regexها سازگار با RE2 هستند (بدون lookahead)؛ نسخهٔ قبلی به‌دلیل استفاده از lookahead عملاً با خطای
`MustCompile` کرش می‌کرد.

| قاعده | هدف |
|---|---|
| `mechanic-jwt-signing-key` | الگوی کلید JWT افشاشده (`Breaking\d+Bad`) |
| `mechanic-arvancloud-keys` | `AccessKey`/`SecretKey` هگز ۱۶+ کاراکتری |
| `mechanic-kavenegar-api-key` | کلید API کاوه‌نگار |
| `mechanic-zarinpal-merchant-id` | `MerchantId` زرین‌پال (بدون GUID صفر/تکراری) |
| `mechanic-database-password-in-connection-string` | رشتهٔ اتصال حاوی `Password=` |
| `mechanic-postgres-uri-credentials` | URIهای `postgres://user:pass@host` |
| `mechanic-production-settings-committed` | ردیابی‌شدن `appsettings.Production/Staging.json` |
| `mechanic-agent-log-dump` | ردیابی‌شدن `otp.json` |

Allowlistهای سراسری فقط مسیرهای غیرراز (خروجی build، `skills/`، `docs/`، `Tests/`، `.env` محلی) و
placeholderهای مستند (`CHANGE_ME`، `YOUR_*`، مقادیر خالی) را کنار می‌گذارند. یافته‌های **تاریخی** در
`.gitleaksignore` پنهان نشده‌اند؛ به‌صورت `PENDING` گزارش می‌شوند تا بازنویسی تاریخچه انجام شود.

## Pre-commit و CI

دو مسیر معادل برای جلوگیری از commit راز:

| لایه | فایل | فعال‌سازی |
|---|---|---|
| Hook بومی Git (بدون وابستگی) | `.githooks/pre-commit` | `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/install-git-hooks.ps1` |
| چارچوب pre-commit | `.pre-commit-config.yaml` | `pip install pre-commit; pre-commit install` |

Hook ابتدا فایل‌های محیطی (`.env`، `appsettings.Production.json`، `otp.json`) را مسدود می‌کند، سپس اگر gitleaks
نصب باشد آن را روی staged changes اجرا می‌کند و در غیر این صورت اسکنر PowerShell
`scripts/security/scan-secrets.ps1 -Staged` را اجرا می‌کند.

`.github/workflows/ci.yml` (که جایگزین فایل خراب قبلی شده) شامل چهار job است:

| Job | کاری که می‌کند |
|---|---|
| `secret-scan` | gitleaks روی بازهٔ commitهای push/PR (blocking) |
| `secret-history-scan` | gitleaks روی کل تاریخچه؛ هفتگی/دستی، پیگیر بدهی SEC-01 |
| `build-test` | `dotnet build -warnaserror` + کل تست‌ها (Docker runner موجود است) |
| `dependency-audit` | `dotnet list package --vulnerable --include-transitive` (blocking) |

## نگهداری کلیدها و اسرار

| محل | وضعیت | نکته |
|---|---|---|
| `Presentation/appsettings.json` | ردیابی‌شده در git | فقط placeholderهای خالی (`Jwt:Key=""`، `Storage:AccessKey=""`، `Kavenegar:ApiKey=""`، `Auth:OtpPepper=""`)؛ مقدار واقعی از env |
| `.env` (ریشه) | gitignored | تنها محل رازهای محلی؛ قالب commit‌شده `.env.example` است |
| `appsettings.Development.json` / `appsettings.Production.json` | gitignored و untracked | مقادیر محیطی روی ماشین/سرور نگه داشته می‌شوند |
| `otp.json` | حذف‌شده از working tree و Git | لاگ ۳ مگابایتی نشست AI با اعتبارنامه‌های زندهٔ production بود (SEC-01) |
| JWT / OTP Pepper / کلیدهای سرویس | env + Secret Manager | چرخش دوره‌ای ۹۰–۱۸۰ روز — [security-verification.md](security-verification.md) |
| کلیدهای DataProtection | Redis | انقضای ۹۰ روزه؛ بکاپ Redis یعنی بکاپ کلیدها |
| TLS در Production | اعتبارسنجی استارت | `Cache:UseRedis` روشن و رشتهٔ Redis با `ssl=true` الزامی است؛ `TrustServerCertificate=true` فقط برای کانتینر محلی |

## اسناد مرتبط

- تأیید امنیتی، چرخش رازها و چک‌لیست انتشار: [security-verification.md](security-verification.md)
- نقاط قوت امنیتی و نگهبان‌های خودکار (فاز ۶): [security-invariants.md](security-invariants.md)
- گزارش ممیزی و وضعیت فازها: [SECURITY-AUDIT-REPORT.md](../../SECURITY-AUDIT-REPORT.md)
- جریان‌های OTP، JWT و سشن کاربر: `03-modules/identity/auth-security.md` (فاز بعدی داکیومنت‌سازی)
- تنظیمات مرتبط (`Security`، `SecurityHeaders`، `Webhook`): [01-getting-started/configuration.md](../01-getting-started/configuration.md)
- نکات انتشار امن (`SignatureRequired`، TLS): [deployment.md](deployment.md)
