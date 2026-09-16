[بازگشت به فهرست یکپارچه‌ها](README.md)

# بومی‌سازی (Localization)

بومی‌سازی در سه لایه پخش شده است: فرهنگ‌های پشتیبانی‌شده و میان‌افزار RequestLocalization در
`Presentation`، قرارداد و Options در `Application/Localization`، و منبع رشته‌های خطا در
`Infrastructure/Localization` به‌همراه کدهای خطای دامنه در `SharedKernel/Localization`.
نتیجه فعلی: پشتیبانی از دو فرهنگ `fa-IR` و `en-US` روی رشته‌های خطا، بدون استفاده از فایل‌های `.resx`.

## فرهنگ‌ها و میان‌افزار

`AddApplicationLocalization` / `UseApplicationLocalization`
(`Presentation/Common/Extensions/LocalizationExtensions.cs`) — فراخوانی از
`PresentationServiceExtensions.cs` و `MiddlewareExtensions.cs`:

| جنبه | مقدار |
|---|---|
| سکشن تنظیمات | `Localization` — **در هیچ appsettings وجود ندارد**، پس پیش‌فرض‌های کلاس اعمال می‌شوند |
| `LocalizationOptions.DefaultCulture` | `fa-IR` |
| `LocalizationOptions.SupportedCultures` | `["fa-IR", "en-US"]` |
| `LocalizationOptions.ResourcesPath` | `Resources` — تعریف شده اما هیچ فایل منبعی روی دیسک/`.resx` وجود ندارد |
| `FallbackToParentCultures` | `true` (برای Culture و UICulture) |
| Providerهای تشخیص فرهنگ | هدر `Accept-Language`، Query String (`?culture=`) و Cookie |

میان‌افزار `UseRequestLocalization` در ابتدای پایپ‌لاین می‌نشیند و `CultureInfo.CurrentUICulture`
درخواست را تعیین می‌کند؛ همین مقدار تنها نقطه مصرف فرهنگ در کل کد است
(`LocalizedErrorMessageProvider`).

## منبع رشته‌ها

`ErrorMessages` (`Infrastructure/Localization/Resources/ErrorMessages.cs`) دو دیکشنری استاتیک دارد —
`Fa` و `En` — هر یک با **۲۴ کلید** یکسان در قالب `error.{domain}.{name}`:

| دامنه | کلیدها |
|---|---|
| `error.general.*` | `unexpected`, `validation`, `not_found`, `conflict`, `unauthorized`, `forbidden`, `rate_limit`, `concurrency`, `cancelled` (۹ کلید) |
| `error.order.*` | `not_found`, `already_paid` |
| `error.payment.*` | `invalid_amount`, `expired`, `transaction_not_found` |
| `error.wallet.*` | `insufficient_balance`, `transfer_limit_exceeded`, `otp_mismatch` |
| سایر | `error.user.invalid_phone`, `error.security.invalid_otp`, `error.brand.duplicate_name`, `error.category.duplicate_name`, `error.attribute.not_found`, `error.attribute.duplicate`, `error.inventory.insufficient_stock` |

انتخاب فرهنگ در `ForCulture`: نام فرهنگ خالی → `Fa`؛ شروع با `en` (حالت‌ناوابسته) → `En`؛ بقیه → `Fa`.

## سرویس ارائه پیام

`LocalizedErrorMessageProvider` پیاده‌سازی `ILocalizedErrorMessageProvider`
(`Application/Localization/Contracts/`) است و در `AddLocalizationServices` به‌صورت **Singleton** ثبت
می‌شود:

| متد | رفتار |
|---|---|
| `GetMessage(errorCode)` | جست‌وجو در دیکشنری فرهنگ جاری؛ **یافت‌نشدن کلید، خود کد را برمی‌گرداند** |
| `GetMessage(errorCode, args)` | قالب‌بندی `string.Format` با فرهنگ جاری؛ `FormatException` → همان قالب |
| `TryGetMessage(errorCode, out message)` | نسخه بدون پرتاب استثنا |

## کدهای خطای دامنه

`SharedKernel/Localization/DomainErrorCodes.cs` فقط ثابت‌های خطای ماژول Wallet را نگه می‌دارد (بخش‌های
`TopUp`, `Transfer`, `Fraud`, `Withdrawal` و کدهای قدیمی‌تر مانند `INSUFFICIENT_WALLET_BALANCE`) و
Aggregatedهای Wallet در `FailureReason` و Exceptionها از آن استفاده می‌کنند
([03-modules/wallet.md](../03-modules/wallet.md)).

## وضعیت فعلی و شکاف‌ها

- `ILocalizedErrorMessageProvider` در DI ثبت شده اما **هیچ Handler، Validator یا کنترلری در مسیر تولید
  آن را فراخوانی نمی‌کند**؛ مصرف واقعی فقط در پروژه تست‌هاست.
- دو دستگاه کد خطای موازی وجود دارد و نگاشت بین‌شان در کد برقرار نیست: Exceptionها کدهایی مانند
  `INSUFFICIENT_WALLET_BALANCE` یا کدهای `ErrorCode` سراسری (`GEN_VALIDATION`, `GEN_NOT_FOUND`, ...)
  می‌دهند، در حالی که کلیدهای دیکشنری `error.*` شکل دیگری دارند؛ بنابراین حتی با اتصال آینده،
  ترجمه خودکار پیام‌های خطا با وضعیت فعلی ممکن نیست.
- پاسخ‌های خطای API فارسی ثابت می‌دهند (پیام‌های داخل Exceptionهای دامنه) و انتخاب فرهنگ کاربردی
  تغییرشان نمی‌دهد؛ رفتار میان‌افزار فقط `CurrentUICulture` را آماده می‌کند.
- سکشن `Localization` را می‌توان به appsettings اضافه کرد (Bind موجود است)، اما در کد فعلی مشخص نیست
  افزودن فرهنگ سوم بدون منبع رشته چه اثری جز انتخاب `Fa` در `ForCulture` خواهد داشت.

## اسناد مرتبط

- پاکت پاسخ و قالب خطای API: [05-api/conventions.md](../05-api/conventions.md)
- Exceptionهای دامنه و کدها: [02-architecture/domain-modeling.md](../02-architecture/domain-modeling.md)
- خطاهای کیف پول: [03-modules/wallet.md](../03-modules/wallet.md)
- فهرست Optionsها و سکشن‌های تنظیمات: [01-getting-started/configuration.md](../01-getting-started/configuration.md)
