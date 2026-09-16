[بازگشت به فهرست ماژول‌ها](README.md)

# پرداخت و درگاه زرین‌پال (Payments)

ماژول `Payment` چرخه تراکنش را از شروع پرداخت تا تأیید درگاه، وب‌هوک، انقضا و بازپرداخت مدیریت می‌کند و
روش‌های پرداخت (درگاه، پرداخت در محل، کیف پول) را به‌صورت داده نگه می‌دارد. Application آن ده Command و
هفت Query دارد و ۱۷ اکشن در پنج کنترلر ارائه می‌کند. ادامه Saga پس از پرداخت در [orders.md](orders.md) و
کیف پول در [wallet.md](wallet.md) مستند شده‌اند.

## دامنه (`Domain/Payment`)

### Aggregateها

`PaymentTransaction : AggregateRoot<PaymentTransactionId>, IAuditable` با `Authority`, `Gateway`,
`Amount` (از نوع `Money`), `Status`, `RefId`, `Fee`, `ErrorMessage`, `Description`,
`IsVerificationInProgress`, `VerifiedAt`, `ExpiresAt` و کلیدهای خارجی `OrderId`/`UserId`.
ثابت‌ها: `DefaultExpiryMinutes = 20`, `MaxExpiryMinutes = 60`, `MaxDescriptionLength = 500`.

| متد | قاعده/اثر |
|---|---|
| `Initiate` / `InitiateWithId` | مبلغ مثبت، توضیحات حداکثر ۵۰۰ کاراکتر، انقضا در بازه `(0, 60]` دقیقه؛ وضعیت آغازین `Pending`؛ صدور `PaymentInitiatedEvent` |
| `MarkAsSuccess(refId, now, fee)` | فقط از `Pending` یا `Processing`؛ تراکنش موفق دوباره تأیید نمی‌شود (`PaymentAlreadyVerifiedException`)، تراکنش ناموفق/منقضی رد می‌شود؛ `RefId` باید مثبت باشد؛ صدور `PaymentSucceededEvent` |
| `MarkAsFailed(now, errorMessage)` | تراکنش موفق قابل تغییر نیست؛ پیش‌فرض پیام خطا «خطای نامشخص»؛ صدور `PaymentFailedEvent` |
| `Expire(now)` | فقط در وضعیت‌های `Pending`/`Processing`؛ صدور `PaymentExpiredEvent` |
| `Refund(now, reason)` | فقط تراکنش موفق؛ وضعیت به `Refunded`؛ صدور `PaymentRefundedEvent` |
| `CanBeVerified(now)` | ترکیب «در وضعیت قابل تأیید» و «منقضی‌نشده» |

`PaymentMethod : AggregateRoot<PaymentMethodId>, IActivatable, IAuditable, ISoftDeletable` با `Name`,
`Code`, `Fee`, `IconUrl`, `IsActive`, `SortOrder`. متدها: `Create`, `Update`, `Activate`/`Deactivate`
(idempotent)، `RequestDeletion`، `Restore` و `CalculateFee(Money)`.

### ValueObjectهای کلیدی

| ValueObject | نکته |
|---|---|
| `PaymentStatus` | رشته‌ای، نه Enum؛ مقادیر `Pending`, `Processing`, `Success`, `Failed`, `Expired`, `Cancelled`, `Refunded` با نام فارسی و پرچم پایانی |
| `PaymentAuthority` | ۵ تا ۲۰۰ کاراکتر و الزامی |
| `PaymentGateway` | نمونه‌های نام‌دار `Zarinpal`, `Mellat`, `Saman`, `Parsian`, `Pasargad`, `Saderat` (غیرفعال), `Wallet` و `Custom(value)` |
| `PaymentMethodCode` | کدهای `zarinpal-sandbox`, `zarinpal`, `cash-on-delivery`, `wallet` |
| `PaymentMethodFee` | مبلغ ثابت + درصد (۰ تا ۱۰۰)؛ بخش درصد به عدد صحیح گرد می‌شود |

### Exceptionها و Domain Eventها

Exceptionها: `InvalidPaymentAmountException` (`INVALID_PAYMENT_AMOUNT`),
`PaymentAlreadyVerifiedException` (`PAYMENT_ALREADY_VERIFIED`), `PaymentExpiredException` (`PAYMENT_EXPIRED`),
`PaymentNotVerifiableException` (`PAYMENT_NOT_VERIFIABLE`), `PaymentTransactionNotFoundException` (`PAYMENT_TRANSACTION_NOT_FOUND`).

Domain Eventها (۱۰): `PaymentInitiatedEvent`, `PaymentSucceededEvent`, `PaymentFailedEvent`,
`PaymentExpiredEvent`, `PaymentRefundedEvent`, `PaymentMethodCreatedEvent`, `PaymentMethodUpdatedEvent`,
`PaymentMethodActivatedEvent`, `PaymentMethodDeactivatedEvent`, `PaymentMethodDeletedEvent`.

سرویس‌های دامنه: `PaymentDomainService.ExpireStaleTransactions` (انقضای تراکنش‌های Pending قدیمی) و
`PaymentSettlementService` با `ValidateRefundEligibility`, `ProcessRefund`, `ProcessPaymentSuccess`
(تسویه سفارش و انتقال به `Processing`). پوشه `Rules/` وجود ندارد.

## Application (`Application/Payment`)

**۱۰ Command:** `InitiatePayment`, `VerifyPayment`, `ProcessWebhook`, `ExpireStalePayments`,
`AtomicRefundPayment`, `CreatePaymentMethod`, `UpdatePaymentMethod`, `ActivatePaymentMethod`,
`DeactivatePaymentMethod`, `DeletePaymentMethod`.

**۷ Query:** `GetAdminPayments`, `GetPaymentByAuthority`, `GetPaymentStatus`,
`GetPaymentsByOrder`, `GetPaymentMethod`, `GetPaymentMethods`, `GetActivePaymentMethods`.

این ماژول در Application هیچ Event Handler ندارد؛ دو Handler در لایه زیرساخت هستند:
`PaymentSucceededInventoryCommitEventHandler` (تأیید رزرو موجودی سفارش) و
`PaymentSucceededNotificationEventHandler` (اعلان).

## جریان پرداخت (از کد)

1. **شروع** — `POST api/v{version:apiVersion}/payments` با `[Authorize]` → `InitiatePaymentHandler`
   وجود سفارش، تعلق آن به کاربر جاری و نبود پرداخت قبلی را می‌سنجد و
   `IPaymentService.InitiatePaymentAsync(orderId, order.FinalAmount, ip, userId, "")` را صدا می‌زند.
   در `PaymentService` (Infrastructure) اگر تراکنش فعالی موجود باشد لینک پرداخت همان برگردانده می‌شود؛
   در غیر این صورت درگاه از `PaymentGatewayFactory` گرفته می‌شود، یک **nonce** ۳۰ دقیقه‌ای صادر
   (`IPaymentCallbackNonceService`) و آدرس بازگشت به‌صورت
   `{FrontendBaseUrl}/payment/callback?paymentId={id:N}&nonce={nonce}` ساخته می‌شود (آدرس فرانت‌اند، نه API).
   سپس `PaymentTransaction.InitiateWithId` اجرا و در صورت `Created` بودن سفارش، `order.MoveToPending()` صدا زده می‌شود.
2. **هدایت به درگاه** — خروجی `PaymentInitiationResult(Authority, PaymentUrl, PaymentTransactionId)` و
   `PaymentUrl` برابر `{StartPayBaseUrl}/{authority}` است.
3. **بازگشت و تأیید** — `GET api/v{version:apiVersion}/payments/verify?authority=&status=` (**بدون `[Authorize]`**،
   چون مرورگر کاربر به آن بازمی‌گردد) → `VerifyPaymentHandler`: اگر `status != "OK"` باشد پاسخ «پرداخت توسط
   کاربر لغو شد» بدون تغییر وضعیت؛ در غیر این صورت `PaymentService.VerifyPaymentAsync` تراکنش را با
   `Authority` می‌خواند، در صورت موفق‌بودن قبلی مسیر Idempotent را می‌رود، `CanBeVerified` را می‌سنجد،
   `gateway.VerifyAsync` (کدهای ۱۰۰ و ۱۰۱ موفق) را صدا می‌زند، `RefId` مثبت را الزام می‌کند،
   `MarkAsSuccess` (**`PaymentSucceededEvent`**) و `order.MarkAsPaid` را اجرا و ذخیره می‌کند.
4. **وب‌هوک** — `POST api/v{version:apiVersion}/payments/webhooks/{gateway}` بدون اتریبیوت احراز هویت →
   `ProcessWebhookHandler` → `PaymentService.ProcessWebhookAsync(authority, status, nonce)` با دو لایه امنیت:
   - `WebhookIpWhitelistMiddleware` با فهرست `"Zarinpal:AllowedIps"`؛ اعمال آن **فقط** وقتی فلگ
     `FeatureFlags.PaymentCallbackIpWhitelistRequired` روشن باشد، وگرنه فقط هشدار لاگ می‌شود (رد = ۴۰۳).
   - اعتبارسنجی nonce؛ اعمال **فقط** با فلگ `FeatureFlags.PaymentCallbackSignatureRequired` و مقایسه
     زمان‌ثابت به‌همراه مصرف اتمیک (get-delete در Redis).
   با `status == "OK"` همان مسیر تأیید کامل اجرا می‌شود و در غیر آن اگر تراکنش `Pending` باشد
   `MarkAsFailed($"Webhook status: {status}")` صدا زده می‌شود.
5. **انقضا** — `PaymentCleanupJob` هر ۵ دقیقه با قفل توزیع‌شده، تراکنش‌های قدیمی‌تر از ۲۰ دقیقه را با
   `ExpireStalePaymentsCommand` منقضی می‌کند (`PaymentExpiredEvent`).
6. **بازپرداخت** — `POST api/v{version:apiVersion}/admin/payments/{id:guid}/refunds` →
   `AtomicRefundPaymentCommand`: احراز شرایط با `PaymentSettlementService` (سفارش پرداخت‌شده یا تحویل‌شده،
   تراکنش موفق و تعلق به همان سفارش)، سپس `payment.Refund()` و `order.Refund()`.

## API

جمع: **۵ کنترلر و ۱۷ اکشن**. قراردادهای عمومی در [05-api/conventions.md](../05-api/conventions.md).
هیچ اتریبیوت Rate Limit اختصاصی در این ماژول ثبت نشده است.

| کنترلر | مسیر (verbatim) | دسترسی | اکشن‌ها |
|---|---|---|---|
| `PaymentsController` | `[Route("api/v{version:apiVersion}/payments")]` | بدون اتریبیوت کلاس؛ هر اکشن جدا | `GET verify` → `VerifyPayment` (عمومی)، `GET {authority}` → `GetByAuthority`، `GET orders/{orderId:guid}` → `GetPaymentsByOrder`، `GET {authority}/status` → `GetPaymentStatus`، `POST` → `InitiatePayment`، `POST webhooks/{gateway}` → `Webhook` (عمومی) |
| `AdminPaymentsController` | `[Route("api/v{version:apiVersion}/admin/payments")]` | `[Authorize(Roles = "Admin")]` | `GET` → `GetPayments`، `POST {id:guid}/refunds` → `RefundPayment` |
| `PaymentMethodsController` | `[Route("api/v{version:apiVersion}/payment-methods")]` | `[AllowAnonymous]` | `GET` → `GetActivePaymentMethods(orderAmount)` |
| `AdminPaymentMethodsController` | `[Route("api/v{version:apiVersion}/admin/payment-methods")]` | `[Authorize(Roles = "Admin")]` | `GET`، `GET {id:guid}`، `POST`، `PUT {id:guid}`، `POST {id:guid}/activate`، `POST {id:guid}/deactivate`، `DELETE {id:guid}` |
| `MockGatewayController` | `[Route("api/v{version:apiVersion}/mock-gateway")]` | بدون احراز هویت | `GET` → `Index` (فقط در محیط Development؛ در غیر آن ۴۰۴) |

## زیرساخت

جزئیات لایه فنی درگاه — پیاده‌سازی‌های `ZarinPalPaymentGateway`/`ZarinPalSandboxGateway`، انتخاب درگاه با
`PaymentGatewayFactory`، تنظیمات `ZarinPalOptions`، سرویس nonce بازگشت، درگاه Mock و
`ZarinPalHealthCheck` — در [04-integrations/payment-gateways.md](../04-integrations/payment-gateways.md)
مستند شده و در این سند تکرار نمی‌شود. بخش اختصاصی ماژول:

- نگاشت EF: ایندکس یگانه روی `Authority`، ایندکس‌های `OrderId`, `UserId`, `(Status, CreatedAt)` و
  مبلغ `decimal(18,2)`.
- `PaymentMethodSeeder` چهار کد روش پرداخت را هنگام استارت می‌کارد؛ فهرست کامل Seederها و Jobها در
  [04-integrations/background-jobs.md](../04-integrations/background-jobs.md) آمده است.

## نکات و محدودیت‌ها

- **بازپرداخت سمت درگاه فراخوانی نمی‌شود**: `Refund()` فقط وضعیت تراکنش و سفارش را در سیستم تغییر می‌دهد
  و در کد فراخوانی API بازپرداخت زرین‌پال دیده نمی‌شود.
- وضعیت `Processing` و `Cancelled` در `PaymentStatus` تعریف شده‌اند اما هیچ متد دامنه‌ای آن‌ها را
  تنظیم نمی‌کند (فقط در بررسی‌ها خوانده می‌شوند)؛ جزئیات مسیر `Processing` در کد فعلی مشخص نیست.
- در ساخت رویدادهای `PaymentSucceededEvent` و `PaymentRefundedEvent` شناسه کاربر به‌صورت
  `UserId.NewId()` ساخته می‌شود، یعنی **شناسه کاربر واقعی تراکنش در این رویدادها منتقل نمی‌شود**.
- حفاظت IP وب‌هوک و امضای nonce هر دو پشت فلگ FeatureManagement هستند؛ در حالت پیش‌فرض خاموش،
  وب‌هوک فقط با nonce قابل حدس‌ناپذیر محافظت می‌شود.
- آدرس بازگشت پرداخت به فرانت‌اند (`{FrontendBaseUrl}/payment/callback`) اشاره می‌کند، نه به API؛
  یعنی تأیید نهایی از سمت کلاینت فرانت‌اند آغاز می‌شود (به‌جز مسیر وب‌هوک).
- `PaymentSucceededEvent` هم‌زمان دو مصرف‌کننده دارد: تأیید موجودی (Infrastructure) و شارژ کیف پول
  (Application/Wallet) — به [wallet.md](wallet.md) مراجعه کنید.

## اسناد مرتبط

- Saga سفارش و ادامه پس از پرداخت: [orders.md](orders.md)
- لایه فنی درگاه زرین‌پال، Factory و Mock: [04-integrations/payment-gateways.md](../04-integrations/payment-gateways.md)
- کیف پول، بازپرداخت به کیف پول و رزرو: [wallet.md](wallet.md)
- تأیید موجودی و رزروها: [inventory.md](inventory.md)
- Outbox، Jobها و Health Checkها: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
- IP Whitelist و هدرهای امنیتی: [07-operations/security.md](../07-operations/security.md)
