[بازگشت به فهرست یکپارچه‌ها](README.md)

# کش و Redis

این سند لایه `Infrastructure/Cache` و نحوه استفاده از Redis را مستند می‌کند: ثبت DI دوحالته،
پیاده‌سازی‌های `ICacheService`، رمزنگاری محتوا، idempotency، قفل توزیع‌شده، ابطال کش واکنشی به رویدادها
و Health Check. نمای کلی (Outbox، Jobها، DataProtection) در
[02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md) آمده و اینجا فقط از منظر Redis
پوشش می‌شود.

## ثبت در DI و سوییچ حالت

همه در `AddCaching` داخل `InfrastructureServiceExtensions.cs` بر اساس `Cache:UseRedis` انجام می‌شود:

| کامپوننت | حالت Redis روشن | حالت Redis خاموش |
|---|---|---|
| اتصال | `IConnectionMultiplexer` (Singleton) + `AddStackExchangeRedisCache` با `InstanceName = KeyPrefix` | `AddMemoryCache` + `AddDistributedMemoryCache` |
| `ICacheService` | `RedisCacheService` و در صورت فعال‌بودن رمزنگاری، پوشش با `EncryptedRedisCacheService` | `InMemoryCacheService` |
| `IDistributedLock` | `DistributedLockService` (Singleton) | `NoOpDistributedLock` (Singleton) |
| `IIdempotencyService` | `RedisIdempotencyService` | `CacheIdempotencyService` |
| Rate Limit | `RateLimitService`, `InMemoryRateLimitService`, `ResilientRateLimitService` به‌صورت نوع عینی Scoped | `IRateLimitService` → `InMemoryRateLimitService` |

- رشته اتصال Redis به‌ترتیب از `ConnectionStrings:Redis`، سپس `Cache:RedisConnectionString` و در نبود
  هر دو مقدار `localhost:6379` خوانده می‌شود.
- `CacheInvalidationService` مستقل از حالت همیشه Scoped ثبت می‌شود.
- در حالت Redis روشن، `IRateLimitService` به‌صورت اینترفیس ثبت نمی‌شود (فقط انواع عینی)؛ مصرف‌کننده‌هایی
  که با `GetRequiredService<IRateLimitService>()` آن را می‌گیرند (`RateLimitMiddleware` و فیلترهای
  Rate Limit) در این حالت به خطای حل وابستگی می‌رسند.

### `CacheOptions` (سکشن `Cache`)

| فیلد | پیش‌فرض کد | نقش |
|---|---|---|
| `IsEnabled` | `false` | خاموش/روشن کلی کش (ملاک `RedisCacheHealthCheck`) |
| `UseRedis` | `false` | انتخاب پیاده‌سازی‌ها (جدول بالا) |
| `RedisConnectionString` | رشته خالی | جایگزین `ConnectionStrings:Redis` |
| `DefaultExpirationMinutes` | ۳۰ | TTL پیش‌فرض `RedisCacheService` |
| `ShortExpirationMinutes` | ۵ | تعریف‌شده؛ مصرفی در `Infrastructure/Cache` ندارد |
| `LongExpirationMinutes` | ۱۲۰ | تعریف‌شده؛ مصرفی در `Infrastructure/Cache` ندارد |
| `KeyPrefix` | `shop` | پیشوند همه کلیدها |

کلیدهای `DefaultTtlMinutes` و `LockTtlSeconds` که در `appsettings.json` زیر `Cache` وجود دارند با هیچ
خاصیتی از `CacheOptions` هم‌نام نیستند و در کد اثری ندارند.

## پیاده‌سازی‌های `ICacheService`

| کلاس | مسیر | رفتار |
|---|---|---|
| `RedisCacheService` | `Infrastructure/Cache/Redis/Services/` | سریال‌سازی JSON با `camelCase`، کلید `{KeyPrefix}:{key}`، TTL پیش‌فرض ۳۰ دقیقه، `RemoveByPrefixAsync` با `server.KeysAsync` و حذف دسته‌ای؛ خطاهای Redis فقط لاگ Warning می‌شوند و متد نتیجه عادی می‌دهد |
| `EncryptedRedisCacheService` | همان پوشه | Decorator رمزنگاری (پایین) |
| `InMemoryCacheService` | `Infrastructure/Cache/Services/` | `IMemoryCache` با فهرست کلیدهای ردیابی‌شده برای حذف با پیشوند |
| `NoOpCacheService` | همان پوشه | همه عملیات no-op با لاگ Debug |

### رمزنگاری محتوای کش

`EncryptedRedisCacheService` فقط وقتی فعال می‌شود که `CacheEncryptionOptions.IsEnabled` باشد
(سکشن `Cache:Encryption`; `KeyBase64` الزاماً کلید ۳۲ بایتی، `KeyId` پیش‌فرض `v1`):

- الگوریتم AES-GCM با nonce ۱۲ بایتی و tag ۱۶ بایتی؛ payload شامل `Version`، `KeyId`، nonce/متن رمز/tag
  به‌صورت base64 و نسخه پشتیبانی‌شده `1` است.
- فقط نوع‌هایی رمز می‌شوند که در کلاس یا ویژگی‌هایشان `[Sensitive]` (`SharedKernel/Attributes/SensitiveAttribute.cs`)
  باشد؛ **در کد فعلی هیچ نوعی این اتریبیوت را ندارد**، پس در عمل هیچ مقداری رمز نمی‌شود.
- `RemoveAsync`/`ExistsAsync` بدون تغییر به لایه داخلی منتقل می‌شوند.

## Idempotency

| کلاس | کلید | TTL | جزئیات |
|---|---|---|---|
| `RedisIdempotencyService` | `{KeyPrefix}:idem:{guid:N}` | ۲۴ ساعت | پاکت شامل نتیجه و زمان پردازش؛ ذخیره با `When.NotExists` |
| `CacheIdempotencyService` | `idempotency:{guid:N}` | ۲۴ ساعت | روی همان `ICacheService` فعال |

مصرف‌کننده هر دو، `IdempotencyBehavior` در پایپ‌لاین MediatR است
([02-architecture/cqrs-features.md](../02-architecture/cqrs-features.md)).

## قفل توزیع‌شده

- قرارداد `IDistributedLock` در `Infrastructure/DistributedLocking/`؛ پیاده‌سازی اصلی
  `DistributedLockService` در `Infrastructure/Cache/Services/`:
  کلید `lock:{resource}` با `SET key token NX EX` (token یک GUID) گرفته می‌شود.
- آزادسازی در `RedisLockHandle` (`Infrastructure/Cache/Redis/Lock/`) با اسکریپت Lua «مقایسه token و سپس
  حذف» انجام می‌شود تا قفل دیگری آزاد نشود.
- `NoOpDistributedLock`/`NoOpLockHandle` در حالت خاموش Redis همیشه قفل موفق می‌دهند.
- کلاس `DistributedLockException` در همان پوشه تعریف شده اما در کد فعلی هیچ‌جا پرتاب نمی‌شود.
- بزرگ‌ترین مصرف‌کننده، کلاس پایه Jobها (`DistributedLockedBackgroundService`) و تک‌تک Jobهای
  [background-jobs.md](background-jobs.md) هستند.

## کش خواندن و ابطال واکنشی

- سمت خواندن، `CachingBehavior` هر Query که `ICacheableQuery` باشد را با `CacheKey` و `Expiry` خودش
  کش می‌کند (الگوی query cache در [02-architecture/cqrs-features.md](../02-architecture/cqrs-features.md)).
- `CacheInvalidationService` با الگوهای `CacheKeys` (`Application/Cache/Features/Shared/`) کار می‌کند:
  `InvalidateProductCacheAsync` (کلید محصول + پیشوند `products:`)، `InvalidateUserCacheAsync` و
  `InvalidateInventoryCacheAsync`.

Handlerهای ابطال در `Infrastructure/Cache/EventHandlers/`:

| Handler | رویدادها | اثر |
|---|---|---|
| `ProductCacheInvalidationHandler` | `ProductUpdatedEvent`, `PriceChangedEvent`, `ProductActivatedEvent`, `ProductDeactivatedEvent` | ابطال کش محصول |
| `OrderCacheInvalidationHandler` | `OrderCreatedEvent`, `OrderPaidEvent`, `OrderCancelledEvent`, `OrderStatusChangedEvent` | ابطال `orders:user:{userId}` و پیشوند `order:{orderId}` |
| `InventoryStockChangedCacheHandler` | پنج رویداد `Stock*` انبار | ابطال کش موجودی تنوع + لاگ `CacheEvent` |
| `VariantStockCacheInvalidationHandler` | `VariantStockChangedApplicationNotification` | حذف `inventory:availability:{variantId}` و `inventory:product-availability:{productId}` و درج دوباره با TTL ۲ دقیقه و پرچم کمبود موجودی (≤ ۵) |

## Health Check

`RedisCacheHealthCheck` (`Infrastructure/Cache/Health/`) وقتی `Cache:IsEnabled` باشد اجرا می‌شود:

- `PingAsync` با اندازه‌گیری تأخیر + نوشتن/خواندن مقدار آزمایشی روی کلید `health:ping` (TTL ۵ ثانیه).
- تأخیر کل بیشتر از ۱۰۰ میلی‌ثانیه → `Degraded` با داده‌های `PingLatency`، `CheckLatency`،
  `ConnectedEndpoints` و `IsConnected`؛ ناهم‌خوانی نوشتن/خواندن → `Degraded`؛ خطای اتصال → `Unhealthy`.
- اگر `Cache:IsEnabled=false` باشد بدون لمس Redis، `Healthy` با پیام «Cache is disabled» برمی‌گردد.

## نکات و محدودیت‌ها

- خطاهای Redis در `RedisCacheService` بلعیده می‌شوند؛ قطعی کش هرگز درخواست را شکست نمی‌دهد اما هیچ
  متریک اختصاصی برای نرخ خطای کش وجود ندارد.
- `RemoveByPrefixAsync` با اسکن کلیدها (`KEYS` سمت سرور از طریق `KeysAsync`) انجام می‌شود؛ روی
  دیتاست بزرگ گران است.
- کش اعلان‌ها و سشن‌ها در این لایه نیست؛ مثلاً `NotificationQueryService` کش ندارد
  ([03-modules/notifications.md](../03-modules/notifications.md)).
- نگهداری کلیدهای DataProtection در Redis و رفتار Resilient آن در
  [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md) و
  [07-operations/security.md](../07-operations/security.md) آمده است.

## اسناد مرتبط

- قفل‌ها و Outbox در نمای معماری: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
- تنظیمات `Cache` و `Cache:Encryption`: [01-getting-started/configuration.md](../01-getting-started/configuration.md)
- Jobهای وابسته به قفل توزیع‌شده: [background-jobs.md](background-jobs.md)
- nonce بازگشت پرداخت روی Redis: [payment-gateways.md](payment-gateways.md)
