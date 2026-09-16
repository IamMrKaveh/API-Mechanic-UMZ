# امنیت عملیاتی

این سند فقط امور عملیاتی امنیت را پوشش می‌دهد: زیرساخت DataProtection، ماژول `Infrastructure/Security`،
ابزارهای اسکن اسرار (gitleaks و pre-commit) و نگهداری کلیدها. جریان‌های احراز هویت، OTP و سشن‌های کاربر
موضوع سند آینده `03-modules/identity/auth-security.md` در فاز بعدی داکیومنت‌سازی هستند و اینجا فقط ارجاع می‌شوند.

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
| تنظیمات | `SecuritySettings` (سکشن `Security`: `AdminIpWhitelist`)، `GoogleAuthSettings` | Bind از افزونه‌های Presentation |

تولید توکن JWT در ماژول Auth و نه Security است (`Infrastructure/Auth/Services/JwtTokenGenerator.cs`).

## Middlewareهای مرتبط با امنیت در Pipeline

| Middleware | نقش عملیاتی |
|---|---|
| `SecurityHeadersMiddleware` | ست‌کردن CSP، HSTS، `X-Frame-Options`، `Referrer-Policy` و `Permissions-Policy` طبق `SecurityHeadersOptions` |
| `UseAdminIpWhitelist` | محدودکردن مسیرهای ادمین به `Security:AdminIpWhitelist` |
| `WebhookIpWhitelistMiddleware` | پذیرش Callbackهای درگاه پرداخت فقط از IPهای مجاز (`Webhook:AllowedPaths`) |
| `SessionActivityMiddleware` | ردیابی آخرین فعالیت سشن‌های کاربر (جزئیات سشن: سند آینده `03-modules/identity/auth-security.md`) |
| `UseApplicationAntiforgery` | محافظت ضد CSRF |

## اسکن اسرار با gitleaks

`.gitleaks.toml` ریشه قواعد پیش‌فرض gitleaks را گسترش می‌دهد:

- ۵ قاعده سفارشی: الگوی JWT جای‌گذاری‌شده (الگوی `Breaking\d+Bad`)، `AccessKey`/`SecretKey` ابر آروان،
  کلید API کاوه‌نگار، `MerchantId` زرین‌پال (GUID) و رشته اتصال PostgreSQL حاوی پسورد.
- Allowlist برای Placeholderهای توسعه (`Password=1234;`، MerchantId صفر، `YOUR_SMTP_*`) و مسیرهای bin/obj.

## pre-commit

`.pre-commit-config.yaml` سه مخزن hook دارد:

| Hook | نسخه | کاری که می‌کند |
|---|---|---|
| `gitleaks` | v8.18.4 | `protect --staged --redact --config .gitleaks.toml` روی هر commit |
| `pre-commit-hooks` | v4.6.0 | `trailing-whitespace`، `end-of-file-fixer`، `check-merge-conflict`، `check-added-large-files` (سقف ۱۰۲۴ کیلوبایت) |
| Local | — | `dotnet format --verify-no-changes --severity warn` روی فایل‌های C# |

## نگهداری کلیدها و اسرار

| محل | وضعیت | نکته |
|---|---|---|
| `Presentation/appsettings.json` | ردیابی‌شده در git | حاوی مقادیر واقعی‌نما برای `Jwt:Key`، کلیدهای S3 و `Kavenegar:ApiKey` است؛ با وجود gitleaks، چرخش این کلیدها و انتقال به مخزن امن (User Secrets یا متغیر محیطی) پیش از انتشار عمومی ضروری است |
| `appsettings.Development.json` / `appsettings.Production.json` | gitignored | مقادیر واقعی محیطی روی هر ماشین/سرور محلی نگه داشته می‌شوند |
| User Secrets | `UserSecretsId` در `Presentation.csproj` | زیرساخت آماده است؛ استفاده فعلی در کد مشخص نیست |
| Azure Key Vault | پکیج‌های `Azure.Security.KeyVault.Secrets` و `Azure.Identity` در `Infrastructure.csproj` | ارجاع‌شده؛ استفاده فعال از آن‌ها در کد فعلی مشخص نیست |
| کلیدهای DataProtection | Redis (بند DataProtection) | انقضای ۹۰ روزه؛ بکاپ گیری از Redis یعنی بکاپ کلیدها |
| `otp.json` ریشه | untracked و در `.gitignore` نیست | فایل ۳ مگابایتی متادیتای سشن یک ابزار بیرونی است و به کد ربطی ندارد (see [configuration.md](../01-getting-started/configuration.md))؛ افزودنش به `.gitignore` توصیه می‌شود |
| TLS در Production | اعتبارسنجی استارت | `Cache:UseRedis` روشن و رشته Redis با `ssl=true` الزامی است |

## اسناد مرتبط

- جریان‌های OTP، JWT و سشن کاربر: `03-modules/identity/auth-security.md` (فاز بعدی داکیومنت‌سازی)
- تنظیمات مرتبط (`Security`، `SecurityHeaders`، `Webhook`): [01-getting-started/configuration.md](../01-getting-started/configuration.md)
- نکات انتشار امن (`SignatureRequired`، TLS): [deployment.md](deployment.md)
