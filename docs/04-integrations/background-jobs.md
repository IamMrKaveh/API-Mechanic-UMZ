[بازگشت به فهرست یکپارچه‌ها](README.md)

# کارهای پس‌زمینه (Background Jobs)

این سند فهرست کامل و واقعی Jobهای پس‌زمینه و Seederهای استارتاپی است: نام کلاس، تریگر (دوره تکرار یا
اجرای یک‌باره)، کلید قفل توزیع‌شده و کار واقعی هر یک. الگوی عمومی و Outbox اصلی در
[02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md) مستند شده؛ اینجا فقط فهرست
عملیاتی کامل آمده است.

## الگوی اجرا

- هیچ Job Scheduler خارجی (Hangfire/Quartz) وجود ندارد؛ همه با `BackgroundService`/`IHostedService`
  خام .NET اجرا می‌شوند و ثبت آن‌ها در `AddBackgroundServices` داخل `InfrastructureServiceExtensions.cs` است.
- کلاس پایه `DistributedLockedBackgroundService` (`Infrastructure/BackgroundJobs/Common/`) چرخه
  «گرفتن قفل Redis → اجرا → `Task.Delay(Interval)`» را فراهم می‌کند؛ **فقط دو Job Outbox از آن ارث
  می‌برند** و بقیه همان الگو را دستی پیاده کرده‌اند.
- قفل‌ها با `IDistributedLock` گرفته می‌شوند (کلید نهایی در Redis با پیشوند `lock:`)؛ جزئیات در
  [cache-redis.md](cache-redis.md).
- خطاهای تکرارشونده هر Job با `IAuditService` (نه فقط ILogger) ثبت می‌شوند.
- پوشه‌های `Infrastructure/Payment/BackgroundServices/` و `Infrastructure/Search/BackgroundServices/`
  و `Infrastructure/Media/BackgroundServices/` خالی‌اند؛ Jobهای واقعی همه در `Infrastructure/BackgroundJobs/` هستند.

## فهرست کامل Jobها

دوره‌ها و مقادیر از ثابت‌های داخل کلاس‌ها استخراج شده‌اند.

### زیرساخت پیام و جست‌وجو

| Job | دوره | قفل | کار |
|---|---|---|---|
| `OutboxProcessingJob` | هر ۱۵ ثانیه | `jobs:outbox-processing` (انقضای قفل ۲ دقیقه) | فراخوانی `IOutboxProcessor.ProcessAsync` — فقط این و `OutboxArchiveJob` از `DistributedLockedBackgroundService` ارث می‌برند |
| `OutboxArchiveJob` | هر ۶ ساعت | `jobs:outbox-archive` (۱۵ دقیقه) | انتقال پیام‌های پردازش‌شده قدیمی‌تر از ۳۰ روز (غیر مسموم) به `OutboxArchiveMessages` در دسته‌های ۵۰۰تایی و حذف از جدول اصلی؛ داخل Execution Strategy و تراکنش |
| `ElasticsearchOutboxJob` | از `Elasticsearch:DeadLetterQueue:ProcessIntervalSeconds` (پیش‌فرض ۶۰ ثانیه؛ شروع با تأخیر ۱۵ ثانیه) | `jobs:elasticsearch-outbox` (۵ دقیقه) | درناژ Outbox جست‌وجو و صف مرده `FailedElasticOperations`؛ سقف retry از `Elasticsearch:DeadLetterQueue:MaxRetries` (۵)، دسته از `Elasticsearch:Sync:BatchSize` (۱۰۰) و backoff نمایی `2^(n+1)` تا ۳۰۰ ثانیه؛ فقط وقتی ES فعال است ثبت می‌شود |
| `ElasticsearchSyncJob` | هر ۱ ساعت (شروع با تأخیر ۳۰ ثانیه) | `jobs:elasticsearch-sync` (۳۰ دقیقه) | همگام‌سازی کامل دیتابیس→ES با `ISearchDatabaseSyncService.SyncAsync`؛ فقط وقتی `Elasticsearch:IsEnabled` و `EnableBackgroundSync` هر دو فعال باشند |

### سفارش و موجودی

| Job | دوره | قفل | کار |
|---|---|---|---|
| `ExpiredOrderCleanupJob` | هر ۳۰ دقیقه | `jobs:expired-order-cleanup` (۱۰ دقیقه) | `FindPendingExpiredAsync` (وضعیت‌های Created/Reserved/Pending با عمر بیش از ۳۰ دقیقه) → `order.Expire(...)` و ذخیره؛ لاگ «{n} سفارش منقضی‌شده پردازش شد.» |
| `InventoryReservationExpiryJob` | هر ۵ دقیقه | `jobs:inventory-reservation-expiry` (۱۰ دقیقه) | یافتن ردیف‌های `Reservation` در `StockLedgerEntries` بدون ردیف آزادسازی/تأیید متناظر و قدیمی‌تر از `ReservationExpiry:ExpiryMinutes` (پیش‌فرض ۳۰) → `RollbackReservationsAsync` بر اساس شماره مرجع یا `ReleaseReservationAsync` با دلیل «آزادسازی خودکار رزرو منقضی‌شده» |

### پرداخت

| Job | دوره | قفل | کار |
|---|---|---|---|
| `PaymentCleanupJob` | هر ۵ دقیقه | `jobs:payment-cleanup` (۱۰ دقیقه) | ارسال `ExpireStalePaymentsCommand` با cutoff «۲۰ دقیقه قبل» → انقضای تراکنش‌های معلق |
| `PaymentReconciliationJob` | هر ۶ ساعت | `jobs:payment-reconciliation` (۱ ساعت) | تراکنش‌های `Pending` با عمر بیش از ۱۲ ساعت در دسته‌های ۱۰۰تایی → `gateway.VerifyAsync` از `PaymentGatewayFactory`؛ تأیید → `MarkAsSuccess` (با لاگ هشدار «was PAID but showed Pending»)، رد/خطای درگاه → `MarkAsFailed` |

### کیف پول

| Job | دوره | قفل | کار |
|---|---|---|---|
| `WalletReconciliationJob` | هر ۱ ساعت | `jobs:wallet-reconciliation` (۴۵ دقیقه) | مقایسه `Wallet.Balance` با جمع `WalletLedgerEntries` در دسته‌های ۲۰۰تایی؛ مغایرت بیش از ۰٫۰۱ → ثبت `WalletReconciliationDiscrepancy` در حسابرسی؛ **فقط گزارش، اصلاحی انجام نمی‌دهد** |
| `WalletReservationExpiryJob` | هر ۵ دقیقه | `jobs:wallet-reservation-expiry` (۱۰ دقیقه) | رزروهای `Active` منقضی‌شده (دسته ۵۰) → ارسال `ReleaseWalletReservationCommand` |
| `WalletTopUpCleanupJob` | هر ۵ دقیقه | `jobs:wallet-topup-cleanup` (۴ دقیقه) | شارژهای `Pending` قدیمی‌تر از ۳۰ دقیقه (دسته ۱۰۰) → `MarkFailed("زمان درخواست شارژ منقضی شد (Timeout).")` |
| `FraudDetectionJob` | هر ۱۵ دقیقه (شروع با تأخیر ۲ دقیقه) | `jobs:fraud-detection` (۱۰ دقیقه) | اجرای ۴ قاعده `IFraudDetectionRule` (`HighVelocityRule`, `UnusualAmountRule`, `RapidTopUpWithdrawRule`, `MultipleFailedTopUpRule`) روی کیف پول‌هایی که در ۱ ساعت گذشته ردیف دفتر دارند (دسته ۵۰)؛ هشدار تکراری با دوره خنک ۶ ساعت رد می‌شود و `WalletFraudAlert.Raise` ثبت می‌گردد |

### حسابرسی

| Job | دوره | قفل | کار |
|---|---|---|---|
| `AuditRetentionJob` | هر ۲۴ ساعت (شروع با تأخیر ۵ دقیقه) | `jobs:audit-retention` (۲ ساعت) | سه دسته نگهداشت **هاردکد در کد**: عمومی ۹۰ روز (آرشیو + حذف، دسته ۱۰۰۰)، امنیتی ۷۳۰ روز (همان، دسته‌بندی جدا) و مالی ۲۵۵۵ روز (فقط آرشیو و `MarkAsArchived`، دسته ۵۰۰)؛ مقصد آرشیو `IAuditArchiveStorage` است که در `AddBackgroundServices` بر اساس `Storage:AuditArchive:Provider` بین `S3AuditArchiveStorage` و `FileSystemAuditArchiveStorage` انتخاب می‌شود |
| `AuditHashUpgradeJob` | تطبیقی (شروع با تأخیر ۲ دقیقه) | `jobs:audit-hash-upgrade` (۱ ساعت) | دسته‌های ۵۰۰تایی از لاگ‌های `HashVersion < AuditLog.CurrentHashVersion` → `UpgradeHashVersion()`؛ اگر کاری نکند ۶ ساعت می‌خوابد وگرنه ۳۰ ثانیه |

### فایل و نشست

| Job | دوره | قفل | کار |
|---|---|---|---|
| `OrphanedFileCleanupJob` | هر ۱۲ ساعت (پس از خطا ۱ ساعت) | `jobs:orphaned-file-cleanup` (۲ ساعت) | رسانه‌های حذف‌نرم‌شده با `DeletedAt` قدیمی‌تر از ۲۴ ساعت (دسته ۱۰۰) → حذف فیزیکی از `IStorageService` و حذف ردیف |
| `ExpiredSessionCleanupJob` | هر ۱ ساعت | `jobs:expired-session-cleanup` (۳۰ دقیقه) | نشست‌های فعال منقضی (`GetExpiredActiveSessionsAsync`) → `session.MarkExpired(cutoff)` و ذخیره |

## Seederهای استارتاپ

دو `IHostedService` دیگر که یک‌بار در استارت اجرا می‌شوند و حلقه تکرار ندارند:

| کلاس | کار |
|---|---|
| `OrderStatusSeeder` (`Infrastructure/Order/Seeders/`) | کاردن ۱۲ تعریف `OrderStatus` (نام، آیکون، رنگ، ترتیب، پرچم‌های لغو/ویرایش و پیش‌فرض) اگر وجود نداشته باشند؛ خطا فقط لاگ می‌شود و استارت را نمی‌شکند |
| `PaymentMethodSeeder` (`Infrastructure/Payment/Seeders/`) | کاردن چهار روش پرداخت با کدهای `zarinpal-sandbox`, `zarinpal`, `cash-on-delivery`, `wallet` و توضیحات فارسی |

## نکات و محدودیت‌ها

- دوره‌های Jobها **قابل پیکربندی نیستند** (به‌جز دو مقدار ES)؛ همه در ثابت‌های کلاس‌ها هستند. تنها
  Options مرتبط، `ReservationExpiryOptions` (سکشن `ReservationExpiry`) است.
- مقادیر نگهداشت حسابرسی در `AuditRetentionJob` هاردکد شده‌اند (۹۰/۷۳۰/۲۵۵۵ روز)؛ سکشن `Audit` در
  appsettings با همین اعداد خوانده نمی‌شود — هم‌خوانی آن‌ها اتفاقی است.
- همه Jobها در چندنمونه‌ای (multi-instance) با قفل Redis از اجرای موازی جلوگیری می‌کنند، اما اگر Redis
  خاموش باشد، در حالت `NoOpDistributedLock` هر نمونه مستقل اجرا می‌شود.
- `ElasticsearchOutboxJob`/`ElasticsearchSyncJob` به‌جای `ILogger` از `IAuditService` استفاده می‌کنند و
  خطاهایشان در جدول حسابرسی ذخیره می‌شود.
- هیچ Jobی برای اعلان‌های قدیمی، توکن‌های منقضی درگاه یا ردیف‌های `RequiresManualReconciliation` Saga
  وجود ندارد؛ پیگیری دستی است.

## اسناد مرتبط

- Outbox اصلی، Interceptorها و Health Checkها: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
- قفل توزیع‌شده و Redis: [cache-redis.md](cache-redis.md)
- انقضای پرداخت و تطبیق: [03-modules/payments.md](../03-modules/payments.md)
- انقضای سفارش و Saga: [03-modules/orders.md](../03-modules/orders.md)
- تشخیص تقلب و رزرو کیف پول: [03-modules/wallet.md](../03-modules/wallet.md)
- نگهداشت لاگ حسابرسی: [03-modules/audit.md](../03-modules/audit.md)
- پاک‌سازی فایل‌های بی‌صاحب: [03-modules/media.md](../03-modules/media.md)
