# تنظیمات و پیکربندی

پیکربندی پروژه حول فایل‌های `appsettings*.json` در `Presentation` و الگوی Options (Bind + ValidateOnStart) شکل گرفته است.
این سند فهرست واقعی بخش‌های تنظیمات، همه کلاس‌های Options با مسیرشان، متغیرهای محیطی و وضعیت فایل‌های خاص
مثل `otp.json` و لاگ‌گیری Serilog را مستند می‌کند.

## منابع تنظیمات

`Presentation/Program.cs` پیکربندی را صریحاً می‌سازد: `appsettings.json`، سپس
`appsettings.{Environment}.json` و در انتها متغیرهای محیطی. وضعیت این فایل‌ها:

| فایل | وضعیت git | توضیح |
|---|---|---|
| `Presentation/appsettings.json` | ردیابی‌شده | تنظیمات پایه؛ مقادیر برخی کلیدها (مثل `Jwt:Key` و کلیدهای S3/کاوه‌نگار) واقعی به نظر می‌رسند — نگاه به [07-operations/security.md](../07-operations/security.md) |
| `Presentation/appsettings.Development.json` | gitignored | رشته‌های اتصال PostgreSQL لوکال، روشن‌کردن Swagger |
| `Presentation/appsettings.Production.json` | gitignored | تنظیمات تولید (Swagger خاموش، فلگ امضای Callback پرداخت روشن و ...) |
| User Secrets | — | `Presentation.csproj` یک `UserSecretsId` دارد؛ استفاده فعلی از آن در کد مشخص نیست |
| متغیرهای محیطی | — | قرارداد استاندارد `__` (مثلاً `ConnectionStrings__DefaultConnection` در docker-compose) |

## بخش‌های appsettings.json

فهرست کامل بخش‌های سطح اول فایل پایه (کلید‌های حساس عمداً فقط با نام آمده‌اند):

| بخش | محتوا |
|---|---|
| `ConnectionStrings` | فقط `Redis`؛ رشته‌های PostgreSQL (`DefaultConnection` و ...) در فایل‌های محیطی تعریف می‌شوند |
| `Api` | `PublicBaseUrl` |
| `Audit` | `RetentionDays` (۹۰)، `FinancialRetentionDays` (۲۵۵۵)، `SecurityRetentionDays` (۷۳۰)، `ArchivePath` |
| `Auth` | سیاست‌های OTP و سشن: `OtpLength`، `OtpExpirationMinutes`، `MaxOtpPerWindow`، `MaxFailedOtpAttempts`، `LockoutDurationMinutes`، `SessionExpirationDays` و مشابه |
| `AWS` / `Storage` | دسترسی S3 (`S3Options` و `StorageOptions`) |
| `Cache` | `IsEnabled`، `UseRedis`، TTLها، پیشوند کلید `shop` |
| `Elasticsearch` | `IsEnabled=false` به‌عنوان پیش‌فرض؛ آدرس، احراز هویت، Bulk، CircuitBreaker، DeadLetterQueue، Sync |
| `FeatureManagement` | ۶ فلگ (فهرست پایین) |
| `FrontendUrls` | آدرس فرانت‌اند و مسیرهای بازگشت پرداخت/کیف پول |
| `HealthChecksUI` | فاصله ارزیابی و فاصله اعلان خرابی |
| `InitialAdmin` | `PhoneNumbers[]` برای ساخت ادمین اولیه |
| `Jwt` | `Key`، `Issuer`، `Audience`، مدت اعتبار Access/Refresh |
| `Kavenegar` | `ApiKey`، `Sender`، `OtpTemplate` |
| `OpenTelemetry` | `OtlpEndpoint`، `TraceSamplingRatio` |
| `PaymentGateway` | `DefaultGateway`، `EnableMockGateway` |
| `ReservationExpiry` | `ExpiryMinutes` برای انقضای رزرو موجودی |
| `ReviewSettings` | محدودیت‌های محتوا و Rate Limit اقدامات نظر |
| `Search` | صفحه‌بندی، `FieldBoosts`، تحلیل فارسی (نرمال‌سازی، Stemming، StopWords) |
| `Security` | `AdminIpWhitelist`، `AllowedOrigins`، `MaxRequestSizeInBytes`، `DataProtectionPath` |
| `SecurityHeaders` | `XFrameOptions`، `ReferrerPolicy`، `PermissionsPolicy`، HSTS، `DisableCsp` |
| `Serilog` | **تنظیماتی مرده** — see بخش Serilog پایین |
| `Smtp` | مقادیر Placeholder (در کد فعلی سرویس SMTP مصرف‌کننده‌ای یافت نشد) |
| `Swagger` | `Enabled` |
| `WalletTransfer` | سقف‌ها و OTP انتقال کیف پول |
| `Webhook` | `AllowedPaths[]` برای میان‌افزار IP-whitelist وب‌هوک |
| `Zarinpal` | `MerchantId`، آدرس‌های API/Sandbox، `UseSandbox`، `AllowedIps` |
| بقیه | `AllowedHosts` و تنظیمات استاندارد ASP.NET Core |

### تفاوت فایل‌های محیطی

| مورد | فقط Development | فقط Production |
|---|---|---|
| رشته‌های اتصال | `DefaultConnection`، `PoolerConnection`، `DirectConnection`، `MigrationConnection` (لوکال) | همان کلیدها به سرویس ابری (Supabase Pooler/Direct) |
| `Swagger:Enabled` | `true` | `false` |
| `Payment.Callback.SignatureRequired` | — | `true` |
| `Storage:AuditArchive:Provider` | `FileSystem` | `S3` |
| `Serilog:WriteTo` | موجود (اما بی‌اثر — see Serilog) | — |

## الگوی Options

الگوی ثابت در `InfrastructureServiceExtensions.cs` و افزونه‌های Presentation چنین است:
`services.AddOptions<T>().Bind(config.GetSection("Section")).ValidateDataAnnotations().ValidateOnStart()`.
فهرست کامل کلاس‌های Options واقعی به تفکیک لایه:

### لایه Application

| کلاس | سکشن | مسیر فایل |
|---|---|---|
| `ApiBaseUrlOptions` | `Api` | `Application/Common/Options/ApiBaseUrlOptions.cs` |
| `JwtOptions` | `Jwt` | `Application/Auth/Features/Shared/JwtOptions.cs` |
| `AuthOptions` | `Auth` | `Application/Auth/Features/Shared/AuthOptions.cs` (رجیستر صریح ندارد؛ مصرف‌کننده‌ها با `IOptions` به پیش‌فرض می‌رسند) |
| `InitialAdminOptions` | `InitialAdmin` | قرارداد در `Application/Auth/Contracts/IInitialAdminOptions.cs`، پیاده‌سازی در Infrastructure |
| `ReviewSettings` | `ReviewSettings` | `Application/Review/Configuration/ReviewSettings.cs` |
| `WalletTransferOptions` | `WalletTransfer` | `Application/Wallet/Options/WalletTransferOptions.cs` |
| `LocalizationOptions` | `Localization` | `Application/Localization/Options/LocalizationOptions.cs` (سکشن `Localization` در هیچ appsettings وجود ندارد) |

### لایه Infrastructure

| کلاس | سکشن | مسیر فایل |
|---|---|---|
| `CacheOptions` | `Cache` | `Infrastructure/Cache/Options/CacheOptions.cs` |
| `CacheEncryptionOptions` | `Cache:Encryption` | `Infrastructure/Cache/Options/CacheEncryptionOptions.cs` (سکشن در appsettings پایه نیست) |
| `OtpOptions` | `Otp` | `Infrastructure/Auth/Options/OtpOptions.cs` — ناسازگاری: سکشن `Otp` وجود ندارد و تنظیمات OTP زیر `Auth` است |
| `ReservationExpiryOptions` | `ReservationExpiry` | `Infrastructure/BackgroundJobs/Options/ReservationExpiryOptions.cs` |
| `FrontendUrlsOptions` | `FrontendUrls` | `Infrastructure/Common/Options/FrontendUrlsOptions.cs` |
| `KavenegarOptions` | `Kavenegar` | `Infrastructure/Communication/Options/KavenegarOptions.cs` |
| `ZarinPalOptions` | `Zarinpal` | `Infrastructure/Payment/ZarinPal/Options/ZarinPalOptions.cs` |
| `ElasticsearchOptions` | `Elasticsearch` | `Infrastructure/Search/Options/ElasticsearchOptions.cs` |
| `S3Options` | `AWS:S3` | `Infrastructure/Storage/Options/S3Options.cs` (رجیستر صریح ندارد؛ فقط `S3AuditArchiveStorage` مصرفش می‌کند) |
| `StorageOptions` | `Storage` | `Infrastructure/Storage/Options/StorageOptions.cs` |
| `AntivirusOptions` | `Storage:Antivirus` | `Infrastructure/Storage/Options/AntivirusOptions.cs` (سکشن در appsettings پایه نیست) |
| `ChaosOptions` | `Chaos` | `Infrastructure/Chaos/Options/ChaosOptions.cs`، Bind در افزونه‌های Presentation (سکشن در appsettings پایه نیست) |

دو کلاس تنظیمی دیگر نیز در `Infrastructure/Security/Settings/` زندگی می‌کنند و از افزونه‌های Presentation
با `Configure<>` ثبت می‌شوند: `SecuritySettings` (سکشن `Security`: `AdminIpWhitelist`) و
`GoogleAuthSettings` (سکشن `Authentication:Google`: `ClientId`/`ClientSecret`؛ سکشن در appsettings وجود ندارد).

### لایه Presentation

| کلاس | سکشن | مسیر فایل / ثبت |
|---|---|---|
| `SecurityHeadersOptions` | `SecurityHeaders` | `Presentation/Common/Extensions/OptionsExtensions.cs` |
| `WebhookOptions` | `Webhook` | `Presentation/Common/Options/WebhookOptions.cs` (رجیستر صریح ندارد؛ تزریق مستقیم در `WebhookIpWhitelistMiddleware`) |
| `ProxySettings` | `ReverseProxy` | `Presentation/Common/Options/ProxySettings.cs` (سکشن در appsettings نیست) |
| `FeatureManagement` | `FeatureManagement` | `Microsoft.FeatureManagement` در `Presentation/Common/Extensions/FeatureManagementExtensions.cs` |
| تنظیمات فریم‌ورک | — | `JwtBearerOptions`، `ApiBehaviorOptions`، `ForwardedHeadersOptions`، `RequestLocalizationOptions` — فقط در کد |

### فلگ‌های FeatureManagement

هر ۶ فلگ در `appsettings.json` به‌صورت پیش‌فرض `false` هستند (به‌جز تغییرات فایل Production):

| فلگ | توضیح |
|---|---|
| `AdminWallet.LedgerV2Enabled` | در فایل Production حذف شده است |
| `Idempotency.DistributedLock.Enabled` | قفل توزیع‌شده برای idempotency |
| `Payment.Callback.SignatureRequired` | در Production روشن |
| `Payment.Callback.IpWhitelistRequired` | — |
| `Saga.AutoRefundOnCommitFailure` | — |
| `Storage.PresignedUrl.Enabled` | — |

## خواندن‌های مستقیم (خارج از Options)

| کلید | محل مصرف |
|---|---|
| `ConnectionStrings:DefaultConnection` | `InfrastructureServiceExtensions.cs` و Health Check دیتابیس |
| `ConnectionStrings:MigrationConnection` | `Infrastructure/Persistence/Context/DBContextFactory.cs` (فقط design-time) |
| `ConnectionStrings:Redis` یا `Cache:RedisConnectionString` | اتصال Redis |
| `Storage:AuditArchive:Provider` | انتخاب بین S3 و File System برای آرشیو حسابرسی |
| `Audit:ArchivePath` | `Infrastructure/Audit/Storage/FileSystemAuditArchiveStorage.cs` |
| `Security:AllowedOrigins` | CORS و `CurrentUserService` |
| `OpenTelemetry:OtlpEndpoint` | `Presentation/Common/Extensions/OpenTelemetryExtensions.cs` |
| متغیر `ASPNETCORE_ENVIRONMENT` | تنها متغیر محیطی خوانده‌شده در کد (غیرفعال‌کردن Retry در Development و انتخاب پیکربندی design-time) |

## اعتبارسنجی هنگام استارت

`ValidateRequiredConfiguration` (در `Presentation/Common/Extensions/ConfigurationValidationExtension.cs`،
فراخوانی از `Program.cs`) این موارد را الزامی می‌کند:

- `ConnectionStrings:DefaultConnection`
- بخش `Jwt` با `Key` حداقل ۳۲ بایت، `Issuer` و `Audience`
- بخش `Storage` با `Provider`، `BucketName`، `AccessKey` و `SecretKey`
- `Kavenegar:ApiKey` و `ZarinPal:MerchantId` در صورت وجود آن بخش‌ها
- در محیط Production: روشن‌بودن `Cache:UseRedis` و رشته Redis با `ssl=true`

## Serilog و محل لاگ‌ها

Serilog در `Program.cs` **کاملاً در کد** پیکربندی می‌شود (`UseSerilog(...)`) و `ReadFrom.Configuration` فراخوانی نمی‌شود؛
بنابراین بخش `Serilog` در فایل‌های appsettings (شامل `WriteTo` فایل Development) بی‌اثر است. Sinkهای واقعی:

| Sink | حداقل سطح | جزئیات |
|---|---|---|
| Console | Warning | — |
| `Logs/log-YYYYMMDD.json` | Warning | فرمت Compact JSON، چرخش روزانه، نگهداری ۱۴ فایل، سقف ۵۰ مگابایت |
| `Logs/errors/errors-YYYYMMDD.json` | Error | با ProcessId/ThreadId، نگهداری ۹۰ فایل |

مسیرهای `/health`، `/swagger` و `/favicon.ico` از لاگ درخواست فیلتر می‌شوند و enrichment شامل
`Application="Mechanic.Api"` و محیط است. لاگ‌های قدیمی در پوشه `Presentation/logs` دیده می‌شوند.

## فایل otp.json

برخلاف نامش، `otp.json` ریشه **فایل تنظیمات اپلیکیشن نیست**: هیچ ارجاعی به آن در کد (cs و csproj و json مصرف‌کننده) وجود ندارد
و محتوای ابتدای آن متادیتای سشن یک ابزار بیرونی هوش مصنوعی (شناسه سشن، آمار تغییرات، هزینه token و ...) است.
قابلیت واقعی OTP در `Application/Auth` و `Infrastructure/Auth/Services/OtpService.cs` پیاده شده است.
این فایل حدود ۳ مگابایت حجم دارد، در git ردیابی نمی‌شود اما به `.gitignore` هم اضافه نشده است.

## اسناد مرتبط

- اجرا و اعمال مایگریشن‌ها: [setup-and-run.md](setup-and-run.md)
- نگهداری کلیدها و اسکن اسرار: [07-operations/security.md](../07-operations/security.md)
- متغیرهای محیطی docker-compose و نکات انتشار: [07-operations/deployment.md](../07-operations/deployment.md)
