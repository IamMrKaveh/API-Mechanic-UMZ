[بازگشت به نقشه مستندات](../README.md)

# واژه‌نامه

این واژه‌نامه اصطلاحات تخصصی به‌کاررفته در مستندات و کد پروژه را با معنای **همان‌طور که در این ریپو
به‌کار می‌روند** توضیح می‌دهد — نه تعریف عمومی کتابی. برای هر اصطلاح، نمونه واقعی در کد و سند اصلی که
جزئیات آن را پوشش می‌دهد آمده است.

## مدل دامنه

| اصطلاح | معنا در این پروژه | نمونه در کد | سند |
|---|---|---|---|
| Aggregate / `AggregateRoot` | ریشه سازگاری تراکنشی دامنه؛ همه از کلاس پایه `AggregateRoot<TId>` در `Domain/Common/Abstractions/` ارث می‌برند و رویداد دامنه صادر می‌کنند | `Order`, `PaymentTransaction`, `Cart` | [02-architecture/domain-modeling.md](../02-architecture/domain-modeling.md) |
| Entity | موجودیت با هویت که مستقل ذخیره نمی‌شود؛ پایه `Entity<TId>` در `SharedKernel/Abstractions/` | `OrderItem`, `StockLedgerEntry`, `CartItem` | [02-architecture/domain-modeling.md](../02-architecture/domain-modeling.md) |
| ValueObject | مقدار تغییرناپذیر با قواعد اعتبارسنجی در سازنده؛ پایه در `SharedKernel/Abstractions/ValueObject.cs` | `Money`, `OrderNumber`, `PaymentAuthority`, `FilePath` | [02-architecture/domain-modeling.md](../02-architecture/domain-modeling.md) |
| Domain Event | رویداد صادره از متدهای Aggregate که با Outbox منتشر می‌شود | `OrderCreatedEvent`, `PaymentSucceededEvent` | [02-architecture/domain-modeling.md](../02-architecture/domain-modeling.md) |
| Domain Exception | استثنای دامنه با `ErrorCode` و پیام فارسی؛ پایه در `SharedKernel/Exceptions/` | `InsufficientWalletBalanceException`, `InvalidOrderTransitionException` | [02-architecture/domain-modeling.md](../02-architecture/domain-modeling.md) |
| Smart Enum | نوع وضعیت رشته‌ای/شمارشی با نمونه‌های نام‌دار، پرچم پایانی و منطق انتقال | `OrderStatusValue`, `PaymentStatus` | [03-modules/orders.md](../03-modules/orders.md) |
| Guard | کلاس‌های پیش‌نیازسنجی ورودی در `SharedKernel/Guard/` | `Guard`, `DomainGuard`, `ContractGuard` | [02-architecture/domain-modeling.md](../02-architecture/domain-modeling.md) |
| Specification | الگوی ترکیب شرط‌ها در `SharedKernel/Specifications/` (`And`/`Or`/`Not`) — تعریف شده اما در کد فعلی مصرفی در دامنه ندارد | `ISpecification` | [02-architecture/domain-modeling.md](../02-architecture/domain-modeling.md) |
| Soft Delete | حذف نرم با `ISoftDeletable` روی خود Aggregate و فیلتر سراسری `!IsDeleted` در EF؛ Interceptor جداگانه‌ای ندارد | `Media`, `Order` | [02-architecture/domain-modeling.md](../02-architecture/domain-modeling.md) |
| SKU | شناسه یکتای تنوع محصول در `Domain/Variant`؛ در آیتم سفارش هم لحظه‌برداری می‌شود | `ProductVariants` | [03-modules/variants.md](../03-modules/variants.md) |
| Ledger | دفتر الحاقی رویدادهای مالی/موجودی که فقط اضافه می‌شود | `StockLedgerEntry`, `WalletLedgerEntry` | [03-modules/inventory.md](../03-modules/inventory.md)، [03-modules/wallet.md](../03-modules/wallet.md) |
| Reservation | رزرو موقت منبع (موجودی یا اعتبار کیف پول) که با Job جداگانه منقضی می‌شود | ردیف `Reservation` در `StockLedgerEntry`؛ جدول `WalletReservations` | [03-modules/inventory.md](../03-modules/inventory.md)، [03-modules/wallet.md](../03-modules/wallet.md) |
| Authority | شناسه تراکنش زرین‌پال؛ ValueObject با ایندکس یگانه روی جدول پرداخت | `PaymentAuthority` | [03-modules/payments.md](../03-modules/payments.md) |
| OTP | کد یک‌بارمصرف ورود پیامکی از کاوه‌نگار؛ سیاست‌هایش در سکشن `Auth` | `OtpService` در `Infrastructure/Auth/Services/` | [03-modules/identity/auth-security.md](../03-modules/identity/auth-security.md) |

## معماری برنامه

| اصطلاح | معنا در این پروژه | نمونه در کد | سند |
|---|---|---|---|
| CQRS | جداسازی مسیر نوشتن و خواندن با MediatR؛ ۷ پروژه Solution | `ICommand`, `IQuery` در `Application/Common/Interfaces/` | [02-architecture/cqrs-features.md](../02-architecture/cqrs-features.md) |
| Command | درخواست تغییر وضعیت که `ServiceResult` برمی‌گرداند | `CheckoutFromCart`, `InitiatePayment` | [02-architecture/cqrs-features.md](../02-architecture/cqrs-features.md) |
| Query | درخواست خواندن؛ نسخه قابل کش آن `ICacheableQuery` با `CacheKey` و `Expiry` | `SearchProducts`, `GetStatesQuery` | [02-architecture/cqrs-features.md](../02-architecture/cqrs-features.md) |
| Handler | کلاس اجراکننده Command/Query/رویداد در پوشه Feature خودش | `InitiatePaymentHandler`, `GetStatesHandler` | [02-architecture/cqrs-features.md](../02-architecture/cqrs-features.md) |
| Validator | اعتبارسنجی FluentValidation هم‌نام با Command/Query در همان Feature | `CheckoutFromCartValidator` | [02-architecture/cqrs-features.md](../02-architecture/cqrs-features.md) |
| Pipeline Behavior | رفتارهای زنجیره‌ای MediatR برای اعتبارسنجی، لاگ، کش و idempotency | `CachingBehavior`, `IdempotencyBehavior` در `Application/Common/Behaviors/` | [02-architecture/cqrs-features.md](../02-architecture/cqrs-features.md) |
| Contract / Adapter | قرارداد Application که با Adapter روی سرویس دامنه سوار می‌شود | `BrandUniquenessCheckerAdapter` | [02-architecture/cqrs-features.md](../02-architecture/cqrs-features.md) |
| QueryService | سرویس خواندن سبک خارج از MediatR با Dapper/EF؛ ثبت خودکار با پسوند نام کلاس | `ProductQueryService` | [02-architecture/cqrs-features.md](../02-architecture/cqrs-features.md) |
| Saga / Process Manager | هماهنگ چند مرحله‌ای رویدادمحور با ذخیره وضعیت و جبران‌سازی | `OrderProcessManagerSaga`, `OrderProcessState` | [03-modules/orders.md](../03-modules/orders.md) |
| Idempotency | جلوگیری از اجرای دوباره با کلید یکتا؛ در سه سطح Command، سفارش و پیام ES Outbox | `IIdempotencyService`, `Order.IdempotencyKey` | [04-integrations/cache-redis.md](../04-integrations/cache-redis.md) |

## زیرساخت اجرا

| اصطلاح | معنا در این پروژه | نمونه در کد | سند |
|---|---|---|---|
| Outbox | انتشار اتمیک رویداد دامنه: نوشتن ردیف همراه داده، پردازش دوره‌ای با `SKIP LOCKED` | `OutboxMessage`, `OutboxProcessor`, `DomainEventInterceptor` | [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md) |
| Poisoned Message | پیام Outbox که پس از سقف retry (`IsPoisoned`) کنار گذاشته می‌شود | `OutboxMessage.IsPoisoned` | [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md) |
| Interceptor | `SaveChangesInterceptor`های EF برای برچسب زمان و برداشت رویداد | `AuditableEntityInterceptor`, `DomainEventInterceptor` | [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md) |
| Distributed Lock | قفل چندنمونه‌ای روی Redis با `SET NX EX` و آزادسازی Lua | `IDistributedLock`, `DistributedLockService` | [04-integrations/cache-redis.md](../04-integrations/cache-redis.md) |
| Background Job | `BackgroundService` خام .NET با قفل Redis و حلقه دوره‌ای؛ بدون Hangfire/Quartz فعال | `OutboxProcessingJob`, `FraudDetectionJob` | [04-integrations/background-jobs.md](../04-integrations/background-jobs.md) |
| Seeder | کاردن داده مرجع یک‌باره هنگام استارت به‌صورت `IHostedService` | `OrderStatusSeeder`, `PaymentMethodSeeder` | [04-integrations/background-jobs.md](../04-integrations/background-jobs.md) |
| NoOp (Null Object) | پیاده‌سازی بی‌اثر جایگزین وقتی سرویس بیرونی خاموش است؛ هرگز خطا نمی‌دهد | `NoOpCacheService`, `NoOpDistributedLock`, `NoOpSearchService` | [04-integrations/cache-redis.md](../04-integrations/cache-redis.md) |
| Circuit Breaker | قطع مدار پس از خطاهای پیاپی و تلاش مجدد پس از مهلت؛ هم Polly روی HttpClient و هم کلاس دستی | `ElasticsearchCircuitBreaker`؛ سیاست‌های `AddTransientHttpErrorPolicy` | [04-integrations/elasticsearch.md](../04-integrations/elasticsearch.md) |
| DLQ (Dead Letter Queue) | جدول صف مرده عملیات ایندکس‌گذاری ناموفق Elasticsearch | `FailedElasticOperations`, `ElasticDeadLetterQueue` | [04-integrations/elasticsearch.md](../04-integrations/elasticsearch.md) |
| Feature Flag | کلیدهای `FeatureManagement` در appsettings (۶ فلگ) که رفتار امنیتی/Saga را سوییچ می‌کنند | `FeatureFlags.PaymentCallbackSignatureRequired` | [01-getting-started/configuration.md](../01-getting-started/configuration.md) |
| Health Check | چک آماده‌باش سرویس‌ها با تگ‌های `ready`/`critical` و Endpointهای `/health/*` | `RedisCacheHealthCheck`, `ZarinPalHealthCheck` | [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md) |
| Chaos Engineering | تزریق تصادفی تأخیر/۵۰۳ در محیط غیرتولیدی با `ChaosEngineeringMiddleware` | `ChaosOptions` | [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md) |
| Nonce | مقدار تصادفی تک‌مصرف برای اعتبارسنجی بازگشت/وب‌هوک پرداخت | `PaymentCallbackNonceService` | [04-integrations/payment-gateways.md](../04-integrations/payment-gateways.md) |
| Idempotent | متدی که اجرای دوباره‌اش تغییری نمی‌دهد؛ در متدهای دامنه مانند `MarkAsRead` به‌کار رفته است | `Notification.MarkAsRead`, `RemovePrimary` | [03-modules/notifications.md](../03-modules/notifications.md) |
