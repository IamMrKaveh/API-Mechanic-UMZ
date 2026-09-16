[بازگشت به فهرست یکپارچه‌ها](README.md)

# لایه فنی درگاه‌های پرداخت

این سند فقط لایه فنی اتصال به درگاه را در `Infrastructure/Payment` پوشش می‌دهد: پیاده‌سازی‌های زرین‌پال،
انتخاب درگاه با `PaymentGatewayFactory`، درگاه Mock، سرویس nonce بازگشت، کلاینت‌های HTTP و Health Check.
جریان بیزینسی پرداخت (Commandها، Endpointها، وب‌هوک، Saga سفارش) در
[03-modules/payments.md](../03-modules/payments.md) مستند شده و اینجا تکرار نمی‌شود.

## ساختار پوشه

| مسیر | محتوا |
|---|---|
| `Infrastructure/Payment/ZarinPal/` | `ZarinPalPaymentGateway`, `ZarinPalSandboxGateway`, `ZarinPalErrorMapper`, `ZarinPalValidator`, زیرپوشه‌های `Options/` و `HealthChecks/` |
| `Infrastructure/Payment/Factory/` | `PaymentGatewayFactory` (انتخاب درگاه) |
| `Infrastructure/Payment/Mock/` | `MockPaymentGateway` |
| `Infrastructure/Payment/Services/` | `PaymentService` (هماهنگ‌کننده درگاه با دامنه) و `PaymentCallbackNonceService` |
| `Infrastructure/Payment/EventHandlers/` | دو Handler موفقیت پرداخت (موجودی و اعلان) |
| `Infrastructure/Payment/BackgroundServices/` | **خالی** — Jobهای پرداخت در `Infrastructure/BackgroundJobs/` هستند |
| `Infrastructure/Payment/Configurations/` | نگاشت EF؛ زیرپوشه `NewFolder/` خالی و بی‌استفاده است |

## ثبت در DI

همه در `AddPaymentServices` داخل `Infrastructure/Common/DependencyInjection/InfrastructureServiceExtensions.cs` انجام می‌شود:

| ثبت | نوع | نکته |
|---|---|---|
| `ZarinPalOptions` | Options | Bind سکشن `Zarinpal` با `ValidateDataAnnotations` و `ValidateOnStart` |
| `FrontendUrlsOptions`, `ApiBaseUrlOptions` | Options | بدون `ValidateDataAnnotations` |
| `IPaymentGatewayFactory` | Scoped | `PaymentGatewayFactory` |
| `IPaymentService` | Scoped | `PaymentService` |
| `IPaymentCallbackNonceService` | Scoped | `PaymentCallbackNonceService` |
| `ZarinPalPaymentGateway` | Typed HttpClient | `BaseAddress` از `ApiBaseUrl` (پیش‌فرض `https://api.zarinpal.com/pg/v4/payment/`)، `Timeout` از `TimeoutSeconds`، سیاست تکرار ۲ بار و Circuit Breaker |
| `"ZarinPalSandbox"` | Named HttpClient | `BaseAddress` از `SandboxApiBaseUrl`، فقط سیاست تکرار ۲ بار (بدون Circuit Breaker) |
| `IPaymentGateway` | Scoped × ۲ | همان دو پیاده‌سازی زرین‌پال؛ **`MockPaymentGateway` ثبت نمی‌شود** |

## انتخاب درگاه (`PaymentGatewayFactory`)

`GetGateway(gatewayName)` منطق زیر را اجرا می‌کند:

| گام | رفتار |
|---|---|
| نام خالی | درگاه پیش‌فرض بر اساس `ZarinPalOptions.UseSandbox`: `ZarinpalSandbox` یا `Zarinpal` |
| نرمال‌سازی | حذف `-`/`_` و تشخیص `zarinpal`/`zarinpal-sandbox` (حالت‌ناوابسته) |
| اجبار سندباکس | اگر `UseSandbox=true` و نام در خانواده زرین‌پال باشد، به `ZarinpalSandbox` هدایت می‌شود |
| یافت‌نشدن | `InvalidOperationException` با پیام «درگاه پرداخت '{نام}' یافت نشد.» (در `PaymentService` به `ExternalServiceException` ترجمه می‌شود) |

`GetAvailableGateways()` فهرست `GatewayName` تمام `IPaymentGateway`های ثبت‌شده را برمی‌گرداند (در عمل فقط
`Zarinpal` و `ZarinpalSandbox`). فهرست درگاه‌های نام‌دار دامنه (`PaymentGateway` ValueObject با نمونه‌های
`Mellat`, `Saman`, `Parsian`, `Pasargad`, `Saderat`, `Wallet`) گسترده‌تر از پیاده‌سازی‌های موجود است؛
برای این نام‌ها در کد فعلی پیاده‌سازی درگاهی وجود ندارد.

## پیاده‌سازی‌های زرین‌پال

| مشخصه | `ZarinPalPaymentGateway` | `ZarinPalSandboxGateway` |
|---|---|---|
| `GatewayName` | `Zarinpal` | `ZarinpalSandbox` |
| کلاینت | `HttpClient` تزریقی (Typed) | `IHttpClientFactory.CreateClient("ZarinPalSandbox")` |
| مسیر درخواست | `request.json` (نسبی به `ApiBaseUrl`) | `pg/v4/payment/request.json` |
| مسیر تأیید | `verify.json` | `pg/v4/payment/verify.json` |
| مرچنت | `MerchantId` | `SandboxMerchantId`، سپس `MerchantId`، در نبود هر دو GUID صفر |
| بازگشت نبود BaseAddress | — (از DI می‌آید) | `https://sandbox.zarinpal.com/` |
| توضیح خالی | به درگاه ارسال می‌شود | `Wallet TopUp {orderId}` جایگزین می‌شود |
| متادیتا | `mobile`/`email` اختیاری | در نبود مقدار، `09000000000` و `test@example.com` |
| نگاشت خطا | `ZarinPalErrorMapper` | متد داخلی `MapErrorCode` (زیرمجموعه کدها) |

رفتار مشترک هر دو:

- **تبدیل مبلغ**: `ToRial` مقدار `Money.Amount` را در صورت واحد `IRT`/`TOMAN` در ۱۰ ضرب و با
  `MidpointRounding.AwayFromZero` گرد می‌کند.
- **موفقیت شروع**: کد `100` به‌همراه `Authority` غیرخالی؛ URL نهایی `{StartPayBaseUrl}/{authority}`
  (برای سندباکس `SandboxStartPayBaseUrl`).
- **موفقیت تأیید**: کدهای `100` و `101`؛ خروجی شامل `RefId`، `CardPan` و `Fee`.
- **خطا**: پرتاب `ExternalServiceException` با نام درگاه و کد خطا؛ خطاهای شبکه/ترجمه JSON هم به همان
  استثنا تبدیل و در `IAuditService` ثبت می‌شوند (`[ZarinPal] Initiate exception: ...`).

### کدهای خطای نگاشت‌شده

`ZarinPalErrorMapper` (برای درگاه اصلی) این کدها را به پیام فارسی نگاشت می‌کند:
`100`, `101`, `-9`, `-10`, `-11`, `-12` (تلاش بیش از حد در بازه کوتاه), `-22`, `-50`, `-51`, `-52`, `-53`,
`-54`, `-1`؛ کد ناشناس → «خطای ناشناخته در درگاه پرداخت.».

### `ZarinPalValidator`

اعتبارسنجی مستقل درخواست (حداقل مبلغ ۱۰۰۰ ریال، توضیحات غیرخالی و حداکثر ۵۰۰ کاراکتر، آدرس بازگشت
مطلق) در `Infrastructure/Payment/ZarinPal/ZarinPalValidator.cs` تعریف شده اما **هیچ‌جا در مسیر تولید
فراخوانی نمی‌شود**؛ تنها مصرف‌کننده آن تست‌ها هستند (`Tests/Infrastructure/Payment/ZarinPal/`).

## درگاه Mock

- `MockPaymentGateway` با `GatewayName = "MockGateway"`، `authority` تصادفی و
  `paymentUrl = /mock/pay?authority=...&amount=...`؛ `VerifyAsync` همیشه موفق است
  (`RefId = DateTime.UtcNow.Ticks`، `CardPan = 6037********1234`، `Fee = 0`).
- این کلاس **در DI ثبت نشده**؛ فقط در تست‌ها ساخته می‌شود.
- `MockGatewayController` (`api/v{version:apiVersion}/mock-gateway`) نیز از آن استفاده نمی‌کند و فقط در
  محیط Development یک صفحه HTML با فرم ارسال به `/api/payments/mock/callback` برمی‌گرداند؛ در غیر
  Development پاسخ ۴۰۴ است.
- بخش تنظیمات `PaymentGateway` در `appsettings.json` (`DefaultGateway = ZarinpalSandbox` و
  `EnableMockGateway = false`) در کد **مصرف‌کننده‌ای ندارد**؛ درگاه پیش‌فرض واقعی از
  `ZarinPalOptions.UseSandbox` تعیین می‌شود.

## سرویس nonce بازگشت از درگاه

`PaymentCallbackNonceService` مقایسه و مصرف اتمیک nonce را بر عهده دارد:

| جنبه | جزئیات |
|---|---|
| کلید | `payment:callback:nonce:{transactionId:N}` |
| تولید | ۳۲ بایت تصادفی (`RandomNumberGenerator`) با base64url |
| صدور | `StringSetAsync(..., When.NotExists)`؛ اگر کلید موجود باشد همان nonce قبلی برگردانده می‌شود |
| اعتبارسنجی | `StringGetDeleteAsync` (get–delete اتمیک در Redis) و مقایسه زمان‌ثابت (`CryptographicOperations.FixedTimeEquals`) |
| fallback | در نبود `IConnectionMultiplexer` از `ICacheService` (get + remove) استفاده می‌شود |
| رخداد امنیتی | عدم تطابق → ثبت `PaymentCallbackNonceMismatch`؛ نبود/مصرف‌شده → لاگ هشدار |

TTL در `PaymentService` برابر ۳۰ دقیقه (`NonceTtl`) است و آدرس بازگشت به شکل
`{FrontendBaseUrl}/payment/callback?paymentId={id:N}&nonce={nonce}` ساخته می‌شود. اعمال اجباری
اعتبارسنجی در وب‌هوک پشت فلگ `FeatureFlags.PaymentCallbackSignatureRequired` است — جزئیات بیزینسی و
فلگ‌ها در [03-modules/payments.md](../03-modules/payments.md) و
[07-operations/security.md](../07-operations/security.md).

## Health Check

`ZarinPalHealthCheck` (`Infrastructure/Payment/ZarinPal/HealthChecks/`) یک درخواست `HEAD` به
`SandboxApiBaseUrl` (وقتی `UseSandbox`) یا `ApiBaseUrl` می‌فرستد؛ مهلت ۵ ثانیه، نتیجه ۶۰ ثانیه در
`IMemoryCache` کش می‌شود. `2xx` → Healthy، پاسخ ناموفق → Degraded، استثنا → Unhealthy. ثبت آن در
`AddHealthChecks` با نام `zarinpal`، `failureStatus = Degraded` و تگ‌های `payment`, `external` است
(فهرست کامل چک‌ها در [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)).

## نکات و محدودیت‌ها

- `Httpx`های زرین‌پال تاب‌آوری نامتقارن دارند: کلاینت اصلی ۲ بار retry و Circuit Breaker
  (۵ خطا / ۶۰ ثانیه) دارد، اما کلاینت سندباکس فقط retry دارد.
- `PaymentReconciliationJob` برای تأیید مجدد تراکنش‌های معلق از همین `PaymentGatewayFactory` استفاده
  می‌کند؛ در خطای ارتباطی، تراکنش `MarkAsFailed` می‌شود (به [background-jobs.md](background-jobs.md)).
- با `UseSandbox = true` (پیش‌فرض کد) هر درخواست خانواده زرین‌پال به سندباکس می‌رود؛ در محیط واقعی باید
  این مقدار و مرچنت‌ها بازبینی شوند.
- بازپرداخت سمت درگاه فراخوانی نمی‌شود؛ `PaymentService` متد Refund درگاه ندارد (توضیح بیزینسی در
  [03-modules/payments.md](../03-modules/payments.md)).
- سرویس nonce در حالت سندباکس هم صادر می‌شود؛ مصرف آن تنها با فلگ امضای وب‌هوک اجباری می‌شود.

## اسناد مرتبط

- جریان بیزینسی پرداخت و وب‌هوک: [03-modules/payments.md](../03-modules/payments.md)
- Jobهای پاک‌سازی و تطبیق پرداخت: [background-jobs.md](background-jobs.md)
- IP Whitelist وب‌هوک و فلگ‌های امنیتی: [07-operations/security.md](../07-operations/security.md)
- تنظیمات `Zarinpal` و فلگ‌ها: [01-getting-started/configuration.md](../01-getting-started/configuration.md)
