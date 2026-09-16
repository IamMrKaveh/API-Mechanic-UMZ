[بازگشت به فهرست ماژول‌ها](README.md)

# کیف پول، تراکنش‌ها و تشخیص تقلب (Wallet)

مهم‌ترین و بزرگ‌ترین ماژول دامنه است: مانده و رزرو وجه، بدهکار/بستانکار ساده، درخواست برداشت نقدی با
تأیید ادمین، شارژ از درگاه، انتقال دو مرحله‌ای با OTP، تجمید حساب و پنج قاعده تشخیص تقلب.
Application آن ۲۲ Command و ۱۷ Query و ۱۴ Event Handler دارد، ۲۶ Domain Event صادر می‌کند و API آن
۴۳ اکشن در سه کنترلر است. مرز پرداخت درگاه با این ماژول در [payments.md](payments.md) آمده است.

## دامنه (`Domain/Wallet`)

### Aggregateها (پنج عدد)

**`Wallet`** — `Balance` (از نوع `Money` با ارز پیش‌فرض `IRT`)، `IsActive`، فیلدهای تجمید
(`FreezeReason`/`FrozenAt`/`FrozenBy`)، مجموعه رزروها و درخواست‌های بدهکار.
`ReservedBalance` = مجموع رزروهای فعال و `AvailableBalance = Balance − ReservedBalance`.

| متد | قاعده/اثر |
|---|---|
| `Credit(amount, description, referenceId, now, idempotencyKey?, correlationId?)` | مبلغ مثبت؛ بررسی `IsActive` **نمی‌کند**؛ صدور `WalletCreditedEvent` |
| `Debit(...)` | `EnsureActive`، مبلغ مثبت، `AvailableBalance >= amount` (وگرنه `InsufficientWalletBalanceException`)؛ صدور `WalletDebitedEvent` |
| `CreateReservation(reservationId, amount, purpose, now, expiresAt?)` | کیف فعال، مبلغ مثبت و مانده آزاد کافی؛ صدور `WalletReservationCreatedEvent` |
| `ReleaseReservation(reservationId, now)` | فقط رزرو فعال؛ در نبود شناسه بی‌صدا رد می‌شود؛ صدور `WalletReservationReleasedEvent` |
| `CreateDebitRequest(...)` | کیف فعال و مانده کافی؛ رزروی با هدف `AdminDebitRequest:{requestId}` نگه می‌دارد و `WalletDebitRequest` با انقضا می‌سازد |
| `ApproveDebitRequest` | **فقط مالک کیف پول** (`approvedBy.Equals(OwnerId)`، وگرنه `UnauthorizedWalletDebitApprovalException`)؛ منقضی → علامت `Expired` + آزادسازی رزرو + Exception؛ موفق → آزادسازی رزرو، کسر از `Balance` و صدور `WalletDebitedEvent` با کلید Idempotency `debit-req-approve:{requestId:N}` |
| `RejectDebitRequest` / `CancelDebitRequest` | فقط در وضعیت Pending؛ آزادسازی رزرو و صدور رویداد متناظر |
| `Freeze` / `Unfreeze` | idempotent؛ صدور `WalletFrozenEvent`/`WalletUnfrozenEvent` |

**`WalletTransfer`** — انتقال میان کاربران با ثابت‌های `MinimumAmount = 10_000` و `MaxOtpAttempts = 5`؛
وضعیت‌ها `PendingOtp`, `Completed`, `Cancelled`, `Expired`, `Failed`.
`Initiate` (انتقال به خود ممنوع، مبلغ حداقل ۱۰٬۰۰۰، ذخیره هش OTP) → `VerifyOtp` (منقضی → `Expired`؛
اشتباه → افزایش `OtpAttempts` و در تلاش پنجم `Failed`) → `MarkCompleted`/`Cancel`/`MarkFailed`.

**`WalletWithdrawalRequest`** — درخواست برداشت نقدی با `MinimumAmount = 50_000`، `Iban`, `AccountHolder`,
`ReservationId` و وضعیت‌های `Pending, Approved, Rejected, Paid, Cancelled`.
متدها: `Create`، `Approve` (**پول را جابه‌جا نمی‌کند**)، `Reject`، `MarkPaid` (از Approved **یا** Pending
مجاز است؛ شماره پیگیری بانکی الزامی)، `Cancel` (فقط مالک، فقط Pending).

**`WalletTopUp`** — شارژ از درگاه با `MinimumAmount = 10_000` و وضعیت‌های `Pending, Succeeded, Failed, Cancelled`؛
همه گذارها فقط از `Pending`.

**`WalletFraudAlert`** — هشدار تقلب با `RuleName`, `Severity`, `Status` (`Open, Reviewed, Dismissed`) و
متدهای `Raise`, `MarkAsReviewed`, `Dismiss`.

### Entity و ValueObjectهای کلیدی

- `WalletLedgerEntry` — دفتر مالی رویدادها با `BalanceAfter`, `TransactionType`, `ReferenceId`,
  `IdempotencyKey` (سقف ۲۰۰)، `CorrelationId` و کلیدهای اختیاری `DebitRequestId`/`WithdrawalRequestId`/
  `TransferId`/`TopUpId`. کارخانه‌های `NewCredit`/`NewDebit` و `FromCreditEvent`/`FromDebitEvent`.
- `IbanNumber` — شبا ایرانی ۲۶ کاراکتری با پیشوند `IR` و بررسی checksum (`IranianIban`) و
  `ToMasked()` برای نمایش.
- شناسه‌های strongly-typed: `WalletId`, `WalletReservationId`, `WalletDebitRequestId`, `WalletTransferId`,
  `WalletWithdrawalRequestId`, `WalletTopUpId`, `WalletLedgerEntryId`, `WalletFraudAlertId`.

Enumهای مهم: `WalletTransactionType` (`Credit, Debit, ReservationConfirmed, Refund, TransferIn, TransferOut`)،
`WalletReservationStatus` (`Active, Confirmed, Released`)، `WalletDebitRequestStatus`,
`WalletTransferStatus`, `WalletWithdrawalStatus`, `WalletTopUpStatus`, `FraudAlertSeverity`
(`Low` تا `Critical`)، `WalletReferenceType`، `AdminWalletAdjustmentType`.

### Exceptionها (۱۲)

`InsufficientWalletBalanceException`, `InvalidWalletAmountException`, `InvalidTopUpAmountException`,
`InvalidWalletDebitRequestStatusException`, `InvalidWalletTransferException`,
`UnauthorizedWalletDebitApprovalException`, `WalletDebitRequestExpiredException`,
`WalletDebitRequestNotFoundException`, `WalletInactiveException`, `WalletReservationNotFoundException`,
`WalletTransferLimitExceededException`, `WalletTransferOtpMismatchException`.

### Domain Eventها (۲۶)

`WalletCreatedEvent`, `WalletCreditedEvent`, `WalletDebitedEvent`, `WalletReservationCreatedEvent`,
`WalletReservationReleasedEvent`, `WalletDebitRequestCreatedEvent`, `WalletDebitRequestApprovedEvent`,
`WalletDebitRequestRejectedEvent`, `WalletDebitRequestCancelledEvent`, `WalletFrozenEvent`,
`WalletUnfrozenEvent`, `WalletTopUpInitiatedEvent`, `WalletTopUpSucceededEvent`, `WalletTopUpFailedEvent`,
`WalletTransferInitiatedEvent`, `WalletTransferCompletedEvent`, `WalletTransferFailedEvent`,
`WalletTransferCancelledEvent`, `WithdrawalRequestedEvent`, `WithdrawalApprovedEvent`,
`WithdrawalRejectedEvent`, `WithdrawalPaidEvent`, `WithdrawalCancelledEvent`,
`WalletFraudAlertRaisedEvent`, `WalletFraudAlertReviewedEvent`, `WalletFraudAlertDismissedEvent`.

پوشه‌های `Projections`، `Results` و `Services` خالی‌اند و پوشه `Rules` وجود ندارد.

## زنجیره‌های عملیاتی (به‌ترتیب واقعی کد)

**۱) رزرو و آزادسازی وجه** — `ReserveWalletCommand` (قفل `wallet:{userId}` و بارگذاری با قفل ردیف) →
`Wallet.CreateReservation` → `WalletReservationCreatedEvent`. آزادسازی با `ReleaseWalletReservationCommand`
→ `Wallet.ReleaseReservation`. **Command «تأیید رزرو» وجود ندارد**؛ تأیید یعنی همان `Debit`.

**۲) بدهکار فوری (Debit)** — `DebitWalletCommand` (Endpoint ادمین `debit-immediate`؛ الزام
`ConfirmForceDebit = true` و هدر `X-Force-Confirmation: yes`) → قفل + بررسی Idempotency
(`HasIdempotencyKeyAsync`) → `Wallet.Debit` → `WalletDebitedEvent` → ثبت دفتر با
`PersistWalletLedgerOnDebitHandler`.

**۳) بدهکار با تأیید مالک (Debit Request)** — ادمین درخواست می‌سازد
(`RequestWalletDebitCommand`، انقضای پیش‌فرض ۷۲ ساعت) → رزرو وجه نگه داشته می‌شود و
`WalletDebitRequestCreatedEvent` + اعلان صادر می‌شود → **مالک کیف پول** از Endpoint
`POST wallet/debit-requests/{requestId}/approve` با `ApproveWalletDebitCommand` تأیید می‌کند
(Handler مالکیت را بررسی و دامنه دوباره کنترل می‌کند) → آزادسازی رزرو، کسر از مانده و صدور
`WalletDebitedEvent` سپس `WalletDebitRequestApprovedEvent`. رد (`RejectWalletDebitCommand`) فقط از سمت
مالک است. درخواست‌های منقضی در لحظه تأیید منقضی علامت می‌خورند.

**۴) بستانکار/شارژ ادمین** — `CreditWalletCommand` → قفل + Idempotency + ساخت خودکار کیف در نبود آن؛
اگر کیف تجمید باشد و فراخوان ادمین باشد، خودکار با دلیل `[ADMIN-CREDIT-AUTO-UNFREEZE]` باز می‌شود →
`Wallet.Credit` → ثبت دفتر و اعلان.

**۵) شارژ از درگاه (TopUp)** — `POST wallet/topup/initiate` → `InitiateWalletTopUpCommand`
(قفل `wallet:topup:initiate:{userId}`؛ حداقل ۱۰٬۰۰۰؛ شناسه سفارش ساختگی = `TopUpId`؛ آدرس بازگشت
`{PublicBaseUrl}/api/v1/wallet/topup/callback`) → `WalletTopUp.Initiate` و `MarkAuthorityIssued` →
بازگشت کاربر به `GET wallet/topup/callback` (ناشناس) یا `POST wallet/topup/complete` →
`CompleteWalletTopUpCommand`: ناموفق/لغو → `MarkFailed`/`MarkCancelled`، موفق → `MarkSucceeded` و
**شارژ همزمان (`wallet.Credit`) در همان Handler**؛ `WalletTopUpSucceededCreditHandler` نیز به
`WalletTopUpSucceededEvent` گوش می‌دهد و با کلید Idempotency دوباره `CreditWalletCommand` می‌فرستد.

**۶) انتقال دو مرحله‌ای بین کاربران** —
۱) `InitiateWalletTransferCommand`: یافتن گیرنده با شماره موبایل، منع انتقال به خود، حداقل/حداکثر مبلغ
(از `WalletTransferOptions`)، سقف روزانه (`SumCompletedAmountForDayAsync` در برابر `DailyLimit`) و سقف
تعداد در ساعت؛ تولید OTP شش‌رقمی با عمر ۱۸۰ ثانیه و `WalletTransfer.Initiate` →
**`WalletTransferInitiatedEvent`** و ارسال OTP به موبایل **فرستنده**.
۲) `ConfirmWalletTransferCommand`: قفل ترتیبی دو کیف (`wallet:user:{minUserId}` سپس `{maxUserId}`) →
`transfer.VerifyOtp` (سقف ۵ تلاش) → بدهکار فرستنده و بستانکار گیرنده (ساخت خودکار کیف گیرنده) در یک
تراکنش → `transfer.MarkCompleted` → **`WalletTransferCompletedEvent`**.
۳) `CancelWalletTransferCommand` فقط توسط سازنده و فقط در `PendingOtp`.

**۷) برداشت نقدی (Withdrawal)** — ۱) `RequestWithdrawalCommand`: حداکثر ۵ درخواست Pending برای هر
کاربر، کیف فعال و مانده آزاد کافی؛ رزرو بدون انقضا با هدف `withdrawal-request` + ساخت درخواست →
`WithdrawalRequestedEvent`. ۲) ادمین `ApproveWithdrawalCommand` → `WithdrawalApprovedEvent` (بدون جابه‌جایی پول).
۳) `RejectWithdrawalCommand` → آزادسازی رزرو + `WithdrawalRejectedEvent`. ۴) `MarkWithdrawalPaidCommand` →
آزادسازی رزرو + `Wallet.Debit` با توضیح «برداشت به شماره پیگیری {bankRef}» + `WithdrawalPaidEvent`.
۵) `CancelWithdrawalCommand` (مالک) → آزادسازی رزرو + `WithdrawalCancelledEvent`.

**۸) تجمید و هشدار تقلب** — `FreezeWalletCommand`/`UnfreezeWalletCommand` با اعلان؛
`MarkFraudAlertReviewedCommand`، `DismissFraudAlertCommand` و `ForceFreezeFromFraudAlertCommand`
(فقط هشدار Open؛ تجمید با دلیل `[Force-Freeze from Alert ...]`).

**محاسبه مانده:** `AvailableBalance = Balance − Σ(رزروهای فعال)`؛ `GetWalletBalanceQuery` هر سه مقدار
`CurrentBalance`, `ReservedBalance`, `AvailableBalance` را برمی‌گرداند.

## Application (`Application/Wallet`)

**۲۲ Command:** `ReserveWallet`, `ReleaseWalletReservation`, `CreditWallet`, `DebitWallet`,
`RequestWalletDebit`, `ApproveWalletDebit`, `RejectWalletDebit`, `FreezeWallet`, `UnfreezeWallet`,
`InitiateWalletTopUp`, `CompleteWalletTopUp`, `InitiateWalletTransfer`, `ConfirmWalletTransfer`,
`CancelWalletTransfer`, `RequestWithdrawal`, `ApproveWithdrawal`, `RejectWithdrawal`,
`MarkWithdrawalPaid`, `CancelWithdrawal`, `MarkFraudAlertReviewed`, `DismissFraudAlert`,
`ForceFreezeFromFraudAlert`.

**۱۷ Query:** `GetWalletBalance`, `GetWalletLedger`, `ExportWalletLedger`, `GetWalletStatistics`,
`GetWalletsOverview`, `GetWalletTransfers`, `GetWalletTransferById`, `PreviewWalletTransfer`,
`GetMyWalletDebitRequests`, `GetPendingDebitRequestsByUser`, `GetAdminDebitRequests`, `GetMyWithdrawals`,
`GetPendingWithdrawals`, `GetWithdrawalById`, `GetFraudAlerts`, `GetFraudAlertById`,
`GetOpenFraudAlertsCount`.

**۱۴ Event Handler** (+ دو رکورد `INotification` بدون ناشر):

| Handler | رویداد | اثر |
|---|---|---|
| `PersistWalletLedgerOnCreditHandler` / `PersistWalletLedgerOnDebitHandler` | `WalletCreditedEvent` / `WalletDebitedEvent` | ثبت `WalletLedgerEntry` با `BalanceAfter` |
| `FraudAlertCriticalFreezeHandler` | `WalletFraudAlertRaisedEvent` | در شدت `Critical` ارسال `FreezeWalletCommand` با دلیل `[Auto-Freeze]` |
| `OrderCancelledWalletReleaseEventHandler` | `OrderCancelledEvent` | اگر ردیف دفتر پرداخت کیف وجود دارد `CreditWalletCommand` (بازپرداخت با کلید `refund-order-{orderId}`) وگرنه آزادسازی رزرو |
| `PaymentRefundedWalletEventHandler` | `PaymentRefundedEvent` | `CreditWalletCommand` با نوع `Refund` |
| `PaymentSucceededWalletCreditEventHandler` | `PaymentSucceededEvent` | `CreditWalletCommand` با کلید `payment-topup-{paymentTransactionId}` |
| `WalletTopUpSucceededCreditHandler` | `WalletTopUpSucceededEvent` | `CreditWalletCommand` با Idempotency |
| `SendWalletCreditNotificationHandler`, `SendWalletDebitNotificationHandler`, `SendWalletDebitRequestCreatedNotificationHandler`, `SendWalletFreezeNotificationHandler`, `SendWalletUnfreezeNotificationHandler`, `WithdrawalPaidNotificationHandler`, `WithdrawalRejectedNotificationHandler` | رویدادهای متناظر | ارسال اعلان (به [notifications.md](notifications.md)) |

Saga اختصاصی در این ماژول وجود ندارد؛ تنها Saga سیستم `OrderProcessManagerSaga` است که به کیف پول
تکیه می‌کند ([orders.md](orders.md)).

## تشخیص تقلب (`Domain/Wallet/FraudDetection`)

چهار قاعده با قرارداد `IFraudDetectionRule` (`RuleName` + `EvaluateAsync(FraudEvaluationContext)`):

| قاعده | شرط | شدت |
|---|---|---|
| `HighVelocityRule` | ۱۰ ردیف دفتر در ۱۰ دقیقه | `High` |
| `MultipleFailedTopUpRule` | ۵ شارژ ناموفق در بازه | `Medium` |
| `RapidTopUpWithdrawRule` | ۲+ شارژ و ۱+ برداشت در بازه | `Critical` |
| `UnusualAmountRule` | فقط با میانگین کاربر ≥ ۱۰٬۰۰۰؛ مبلغ ≥ ۱۰ برابر میانگین / ≥ ۵۰ برابر | `High` / `Critical` |

اجرا با `FraudDetectionJob` (`Infrastructure/BackgroundJobs/`): هر ۱۵ دقیقه، قفل
`jobs:fraud-detection`، پنجره ارزیابی یک ساعت، خنک‌سازی ۶ ساعته برای تکرار هشدار، دسته‌های ۵۰ کیفی با
حداکثر ۲۰۰ ردیف دفتر. هشدار `Critical` به‌طور خودکار کیف را تجمید می‌کند؛
`UnusualAmountRule` تنها قاعده‌ای است که به میانگین مبلغ گذشته کاربر نیاز دارد.

## API

جمع: **۳ کنترلر و ۴۳ اکشن**. قراردادهای عمومی در [05-api/conventions.md](../05-api/conventions.md).

`WalletController` — `[Route("api/v{version:apiVersion}/wallet")]` — `[Authorize]`؛ ۱۸ اکشن:

| گروه | اکشن‌ها |
|---|---|
| مانده و دفتر | `GET balance`، `GET ledger` |
| شارژ | `POST topup/initiate`، `GET topup/callback` (`[AllowAnonymous]`)، `POST topup/complete` |
| برداشت | `POST withdrawals`، `GET withdrawals`، `GET withdrawals/{id:guid}`، `POST withdrawals/{id:guid}/cancel` |
| عملیات روی درخواست‌ها | `POST {id:guid}/reject`، `POST {id:guid}/mark-paid` |
| انتقال | `POST transfer/preview`، `POST transfer/initiate`، `POST transfer/confirm`، `POST transfer/{id:guid}/cancel` |
| درخواست بدهکار | `GET debit-requests`، `POST debit-requests/{requestId:guid}/approve`، `POST debit-requests/{requestId:guid}/reject` |

`AdminWalletController` — `[Route("api/v{version:apiVersion}/admin/wallets")]` —
`[Authorize(Roles = "Admin")]` و `[EnableRateLimiting("admin-wallet")]` (پنجره ثابت، ۶۰ درخواست در دقیقه)؛ ۲۰ اکشن:
`GET overview`، `GET statistics`، `GET {userId:guid}/balance`، `GET {userId:guid}/ledger`،
`GET {userId:guid}/ledger/export`، `POST {userId:guid}/credit`، `POST {userId:guid}/debit-immediate`
(الزام هدر `X-Force-Confirmation: yes`)، `POST {userId:guid}/debit-requests`،
`GET {userId:guid}/debit-requests/pending`، `GET debit-requests`، `GET transfers`،
`GET transfers/{id:guid}`، `POST {userId:guid}/freeze`، `POST {userId:guid}/unfreeze`،
`GET fraud/alerts`، `GET fraud/alerts/count-open`، `GET fraud/alerts/{id:guid}`،
`POST fraud/alerts/{id:guid}/mark-reviewed`، `POST fraud/alerts/{id:guid}/dismiss`،
`POST fraud/alerts/{id:guid}/force-freeze`.
در این کنترلر هدر `Idempotency-Key` پشتیبانی و در صورت نبود، کلید خودکار ساخته و هدرهای
`X-Idempotency-Key-Effective` و `X-Correlation-ID` برگردانده می‌شوند.

`AdminWalletWithdrawalController` — `[Route("api/v{version:apiVersion}/admin/wallets/withdrawals")]` —
`[Authorize(Roles = "Admin")]` و `[EnableRateLimiting("admin-wallet")]`؛ ۵ اکشن:
`GET pending`، `GET {id:guid}`، `POST {id:guid}/approve`، `POST {id:guid}/reject`، `POST {id:guid}/mark-paid`.

## زیرساخت

- `WalletConfiguration`: مانده `decimal(18,2)`، ستون `xid` به‌عنوان توکن همزمانی و ایندکس یگانه
  `IX_Wallets_UserId`.
- `WalletLedgerEntryConfiguration`: **ایندکس یگانه فیلترشده روی `IdempotencyKey`**
  (`IX_WalletLedgerEntries_IdempotencyKey`) که تکرار پردازش رویدادها را در سطح دیتابیس می‌بندد؛
  ایندکس‌های `(WalletId, OccurredAt desc)`, `(OwnerId, OccurredAt desc)` و ستون‌های ارجاع.
- `WalletReconciliationAudit` جدول تطبیق مانده (`SnapshotBalance`, `LedgerBalance`, `Delta`) است و
  `WalletReconciliationJob` (به [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md))
  از آن استفاده می‌کند.
- `WalletTransferOptions` (بخش `WalletTransfer`): `MinimumAmount=10000`, `MaximumAmount=1,000,000,000`,
  `DailyLimit=50,000,000`, `OtpLength=6`, `OtpTtlSeconds=180`, `MaxPendingTransfersPerHour=5`, `Currency="IRT"`.
- `WalletRepository.GetByUserIdForUpdateAsync` بارگذاری با قفل ردیف (SELECT ... FOR UPDATE) انجام می‌دهد
  و Handlerهای حساس با `IDistributedLock` قفل `wallet:{userId}` می‌گیرند.
- سه Job مرتبط: `WalletReconciliationJob`، `WalletReservationExpiryJob` و `WalletTopUpCleanupJob`.

## نکات و محدودیت‌ها

- **تأیید بدهکار با مالک کیف پول است نه ادمین**: ادمین فقط درخواست می‌سازد و مالک آن را تأیید/رد می‌کند.
  اکشن‌های `POST wallet/{id}/reject` و `POST wallet/{id}/mark-paid` در `WalletController` (کاربر) قرار
  دارند و در Handlerهای آن‌ها بررسی نقش ادمین دیده نمی‌شود؛ مرز دسترسی این دو اکشن در کد فعلی مشخص نیست.
- `WalletReservationStatus.Confirmed` هرگز توسط هیچ متدی تنظیم نمی‌شود؛ تأیید نگه‌داشت وجه همان `Debit` است.
- `CancelWithdrawalHandler` در پایان `SaveChangesAsync` صدا نمی‌زند؛ بسته به رفتار Pipeline، ذخیره این
  تغییر ممکن است به UoW خارجی وابسته باشد.
- `WalletStatus` (Active/Suspended/Closed) تعریف شده اما Aggregate از `IsActive` بولی استفاده می‌کند؛
  استفاده‌ای از این Enum در کد دیده نمی‌شود.
- آدرس بازگشت TopUp به‌صورت هاردکد `/api/v1/wallet/topup/callback` ساخته می‌شود و مستقل از نسخه API است.
- از `WalletTransactionType` فقط `Credit`, `Debit` و `Refund` در کد تولید می‌شوند؛ سایر مقادیر
  (`ReservationConfirmed`, `TransferIn`, `TransferOut`) در حال حاضر تولید نمی‌شوند.
- دو رکورد `WalletRefundApplicationEvent` و `WalletTopUpApplicationEvent` تعریف شده‌اند اما ناشری
  برای آن‌ها پیدا نشد؛ مسیر مصرفشان در کد فعلی مشخص نیست.
- ارز پیش‌فرض کیف پول `IRT` است ولی `UserCreatedEventHandler` کیف را با `IRR` می‌سازد؛ تفاوت ارز در
  دو نقطه کد وجود دارد.

## اسناد مرتبط

- پرداخت درگاه و بازپرداخت: [payments.md](payments.md)
- Saga سفارش و زنجیره لغو: [orders.md](orders.md)
- تشخیص تقلب در سطح Job: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
- اعلان‌های کیف پول: [notifications.md](notifications.md)
- Rate Limiting ادمین: [05-api/conventions.md](../05-api/conventions.md)
