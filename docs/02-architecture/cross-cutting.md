# دغدغه‌های مشترک (Cross-Cutting)

مجموعه‌ای از زیرساخت‌های فرا-ماژول در `Infrastructure` زندگی می‌کنند: Outbox برای انتشار اتمیک رویدادها،
Interceptorهای Persistence، قفل توزیع‌شده روی Redis، کش و idempotency، Jobهای پس‌زمینه،
Health Checkها و Chaos Engineering. این سند هر یک را با کلاس‌ها و پارامترهای واقعی کد مستند می‌کند.

## Outbox

مسیر کامل انتشار Domain Event به‌صورت اتمیک با ذخیره‌سازی داده:

| مرحله | کامپوننت | جزئیات |
|---|---|---|
| نوشتن | `DomainEventInterceptor` | در `SaveChangesAsync` رویدادهای دامنه را با JSON camelCase، trace context با فرمت W3C (`TraceParent`/`TraceState`) و نام نوع از `IOutboxEventTypeRegistry` به ردیف `OutboxMessage` تبدیل می‌کند |
| ذخیره | `OutboxMessage` | موجودیت جدول `OutboxMessages` با `RetryCount`، `Error`، `IsPoisoned`، `ProcessedAt`؛ آرشیو در جدول `OutboxArchiveMessages` (`OutboxArchiveMessage`) |
| برداشت | `OutboxProcessor.ProcessAsync` | SQL خام `SELECT ... FOR UPDATE SKIP LOCKED` با batch پیش‌فرض ۵۰، سقف ۵ بار retry و سپس علامت‌گذاری `IsPoisoned`؛ انتشار با MediatR `IPublisher` در قالب `DomainEventNotification<T>` و ادامه Activity از trace ذخیره‌شده |
| زمان‌بندی | `OutboxProcessingJob` | هر ۱۵ ثانیه، زیر کلاس `DistributedLockedBackgroundService` با قفل Redis کلید `jobs:outbox-processing` (`Infrastructure/BackgroundJobs/`) |
| آرشیو | `OutboxArchiveJob` | هر ۶ ساعت پیام‌های پردازش‌شده را به جدول آرشیو منتقل می‌کند |

نکات:

- مسیر اصلی کد `Infrastructure/Persistence/Outbox/` است؛ پوشه `Infrastructure/Common/Outbox` وجود دارد اما خالی است.
- یک Outbox دوم و مستقل برای جست‌وجو وجود دارد: `Infrastructure/Search/ElasticsearchOutboxMessage.cs` با
  صف مرده (`ElasticDeadLetterQueue`) و Jobهای `ElasticsearchOutboxJob` (فاصله پیش‌فرض ۶۰ ثانیه) و `ElasticsearchSyncJob`.
- مصرف‌کننده‌های نمونه: `ProductCacheInvalidationHandler` و `OrderCacheInvalidationHandler` در
  `Infrastructure/Cache/EventHandlers/` و سرویس‌های همگام‌سازی `Infrastructure/Search/EventHandlers/`.

## Interceptorهای Persistence

دو `SaveChangesInterceptor` ثبت‌شده در `AddPersistence`:

| Interceptor | نقش |
|---|---|
| `AuditableEntityInterceptor` | ست‌کردن `CreatedAt`/`UpdatedAt` برای `IAuditable` با `IDateTimeProvider`، جلوگیری از تغییر `CreatedAt` و مدیریت توکن همزمانی `RowVersion` |
| `DomainEventInterceptor` | برداشت رویدادهای دامنه و نوشتن Outbox (see بالا) |

Interceptor حذف نرم وجود ندارد؛ حذف نرم در خود Aggregateها با `ISoftDeletable` انجام می‌شود.

## Distributed Locking

- قرارداد: `IDistributedLock` در `Infrastructure/DistributedLocking/IDistributedLock.cs` با
  `AcquireAsync(key, expiry) → ILockHandle?`.
- پیاده‌سازی: `DistributedLockService` در `Infrastructure/Cache/Services/` روی Redis با الگوی
  `SET key token NX EX` و انتشار با اسکریپت Lua (`RedisLockHandle` در `Infrastructure/Cache/Redis/Lock/`).
- هنگام خاموشی Redis، `NoOpDistributedLock` جایگزین می‌شود و خطای قفل با `DistributedLockException` گزارش می‌شود.
- مصرف اصلی: کلاس پایه `DistributedLockedBackgroundService` برای همه Jobها (کلیدهایی مثل `jobs:outbox-processing`)
  و `IdempotencyBehavior` پلیپ‌لاین.

## کش و Idempotency

همه با فلگ `Cache:UseRedis` سوییچ می‌شوند؛ خاموشی Redis فقط به fallback حافظه‌ای می‌رسد، نه شکست درخواست:

| کامپوننت | مسیر | نقش |
|---|---|---|
| `RedisCacheService` | `Infrastructure/Cache/Redis/Services/` | پیاده‌سازی `ICacheService` (get/set/remove/removeByPrefix) |
| `EncryptedRedisCacheService` | همان پوشه | لایه رمزنگاری وقتی `CacheEncryptionOptions.IsEnabled` |
| `RedisIdempotencyService` | `Infrastructure/Cache/` | ذخیره نتیجه Commandهای تکراری برای `IdempotencyBehavior` |
| `InMemoryCacheService` / `NoOpCacheService` / `CacheIdempotencyService` | `Infrastructure/Cache/Services/` | fallbackها |
| `CacheInvalidationService` + Handlerهای Event | `Infrastructure/Cache/` و `Infrastructure/Cache/EventHandlers/` | ابطال کش واکنشی به رویدادهای دامنه |
| `RedisCacheHealthCheck` | `Infrastructure/Cache/Health/` | سلامت Redis (جدول پایین) |

پیشوند کلیدها `shop` و TTLهای پیش‌فرض در `CacheOptions` تعریف شده‌اند (see [01-getting-started/configuration.md](../01-getting-started/configuration.md)).

## Jobهای پس‌زمینه

کتابخانه Job، **`BackgroundService` خام .NET** است (نه Hangfire؛ پکیج Quartz در csproj هست اما در کد استفاده نمی‌شود).
کلاس پایه `DistributedLockedBackgroundService` چرخه «گرفتن قفل Redis → اجرا → `Task.Delay(Interval)`» را با لاگ خطای غیرمرگبار پیاده می‌کند.

| Job | نقش |
|---|---|
| `OutboxProcessingJob` | پردازش Outbox — هر ۱۵ ثانیه |
| `OutboxArchiveJob` | انتقال به آرشیو — هر ۶ ساعت |
| `ElasticsearchOutboxJob` / `ElasticsearchSyncJob` | درناژ Outbox جست‌وجو و همگام‌سازی ایندکس (در صورت فعال‌بودن Elasticsearch) |
| `AuditRetentionJob` / `AuditHashUpgradeJob` | نگهداشت و ارتقای هش لاگ‌های حسابرسی |
| `ExpiredOrderCleanupJob` / `ExpiredSessionCleanupJob` | پاک‌سازی سفارش و سشن منقضی |
| `FraudDetectionJob` | اجرای قواعد تشخیص تقلب کیف پول |
| `InventoryReservationExpiryJob` | انقضای رزروهای موجودی |
| `OrphanedFileCleanupJob` | پاک‌سازی فایل‌های بی‌صاحب |
| `PaymentCleanupJob` / `PaymentReconciliationJob` | پاک‌سازی و تطبیق پرداخت‌ها |
| `WalletReconciliationJob` / `WalletReservationExpiryJob` / `WalletTopUpCleanupJob` | تطبیق، انقضای رزرو و پاک‌سازی شارژ کیف پول |
| `OrderStatusSeeder` / `PaymentMethodSeeder` | Seed داده مرجع هنگام استارت |

## Health Checkها

ثبت در `InfrastructureServiceExtensions.AddHealthChecks` و نگاشت در
`Presentation/Common/Extensions/HealthCheckExtensions.cs`:

| نام | کلاس / منبع | تگ‌ها |
|---|---|---|
| `postgresql` | `AddNpgSql` داخلی پکیج HealthChecks.NpgSql | `db, sql, ready, critical` |
| `redis` | `RedisCacheHealthCheck` (`Infrastructure/Cache/Health/`) | فقط وقتی `Cache:UseRedis` |
| `elasticsearch` | `ElasticsearchHealthCheck` (`Infrastructure/Search/HealthChecks/`) | فقط وقتی ES فعال |
| `elasticsearch-index` | `ElasticsearchIndexHealthCheck` | ایندکس‌های جست‌وجو |
| `elasticsearch-dlq` | `ElasticsearchDLQHealthCheck` | صف مرده ES |
| `kavenegar` | `KavenegarHealthCheck` (`Infrastructure/Communication/HealthChecks/`) | خرابی → Degraded |
| `zarinpal` | `ZarinPalHealthCheck` (`Infrastructure/Payment/ZarinPal/HealthChecks/`) | خرابی → Degraded |

Endpointها: `/health/live` (liveness، ناشناس)، `/health/ready` (فقط تگ `ready`، ناشناس) و
`/health/details` (همه چک‌ها با خروجی UI؛ نیازمند نقش Admin).

## Chaos Engineering (تست خرابی)

تزریق عمدی خرابی برای محیط‌های غیرتولیدی:

- تنظیم: `ChaosOptions` در `Infrastructure/Chaos/Options/ChaosOptions.cs` — `IsEnabled`،
  `LatencyInjectionRate`، `MaxLatencyMilliseconds` (۵۰۰۰)، `FaultInjectionRate` و
  مسیرهای شمول/استثنا (`/health`، `/metrics` و `/swagger` همیشه مستثنا هستند).
- اجرا: `ChaosEngineeringMiddleware` در `Presentation/Common/Middleware/` که به‌صورت تصادفی تأخیر یا HTTP 503 تزریق می‌کند.
- فعال‌سازی فقط با تنظیمات است (نه متغیر محیطی) و در Production با کد (`ChaosExtensions.cs`) اجباراً `IsEnabled=false` می‌شود.

## رؤیت‌پذیری (Observability)

- OpenTelemetry با Exporter OTLP (`OpenTelemetry:OtlpEndpoint`) و instrumentationهای AspNetCore/Http/Runtime
  در `Presentation/Common/Extensions/OpenTelemetryExtensions.cs`.
- `CorrelationIdMiddleware` هدر `X-Correlation-ID` را برای هر درخواست تضمین می‌کند و
  `RequestLoggingMiddleware` لاگ ساختاریافته درخواست می‌نویسد.
- ActivitySourceهای مشترک در `SharedContracts/Diagnostics` تعریف شده‌اند و Outbox با ذخیره
  `traceparent` در پیام‌ها، trace را از سمت صدور رویداد تا مصرف‌کننده ادامه می‌دهد.

## DataProtection

کلیدهای ASP.NET Core Data Protection به‌جای دیسک، به‌صورت XML در Redis (پیشوند `DataProtection`، انقضای ۹۰ روز)
با `ResilientRedisXmlRepository` در `Infrastructure/DataProtection/Repositories/ResilientRedisXmlRepository.cs`
ذخیره می‌شوند؛ «Resilient» یعنی با از کارافتادگی Redis به‌جای exception، لیست خالی برمی‌گردد.
جزئیات عملیاتی و نگهداری کلیدها در [07-operations/security.md](../07-operations/security.md) است.

## اسناد مرتبط

- فلگ‌های FeatureManagement و Options مرتبط: [01-getting-started/configuration.md](../01-getting-started/configuration.md)
- جریان صدور رویداد در دامنه: [domain-modeling.md](domain-modeling.md)
