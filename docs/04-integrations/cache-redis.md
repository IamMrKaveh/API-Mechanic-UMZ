[بازگشت به فهرست یکپارچه‌ها](README.md)

# کش و Redis

این سند لایه `Infrastructure/Cache` و نحوه استفاده از Redis را مستند می‌کند: ثبت DI دوحالته،
کش خروجی MediatR (Output Cache) با ابطال خودکار مبتنی بر EF Core، پیاده‌سازی‌های `ICacheService`،
رمزنگاری محتوا، idempotency، قفل توزیع‌شده و Health Check. نمای کلی (Outbox، Jobها، DataProtection) در
[02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md) آمده و اینجا فقط از منظر Redis
پوشش می‌شود.

## ثبت در DI و سوییچ حالت

همه در `AddCaching` داخل `InfrastructureServiceExtensions.cs` بر اساس `Cache:UseRedis` انجام می‌شود:

| کامپوننت              | حالت Redis روشن                                                                                                 | حالت Redis خاموش                                                         |
| --------------------- | --------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| اتصال                 | `IConnectionMultiplexer` (Singleton) + `AddStackExchangeRedisCache` با `InstanceName = KeyPrefix`               | `AddMemoryCache` + `AddDistributedMemoryCache`                           |
| کش خروجی MediatR      | `AddMediatRResponseCache` با provider ردیس (`InstanceName = {KeyPrefix}:mediatr:` و ابطال توزیع‌شده با Pub/Sub) | `AddMediatRResponseCache` با کش حافظه‌ای درون‌فرایندی (`UseMemoryCache`) |
| `ICacheService`       | `RedisCacheService` و در صورت فعال‌بودن رمزنگاری، پوشش با `EncryptedRedisCacheService`                          | `InMemoryCacheService`                                                   |
| `IDistributedLock`    | `DistributedLockService` (Singleton)                                                                            | `NoOpDistributedLock` (Singleton)                                        |
| `IIdempotencyService` | `RedisIdempotencyService`                                                                                       | `CacheIdempotencyService`                                                |
| Rate Limit            | `RateLimitService`, `InMemoryRateLimitService`, `ResilientRateLimitService` به‌صورت نوع عینی Scoped             | `IRateLimitService` → `InMemoryRateLimitService`                         |

- رشته اتصال Redis به‌ترتیب از `ConnectionStrings:Redis`، سپس `Cache:RedisConnectionString` و در نبود
  هر دو مقدار `localhost:6379` خوانده می‌شود.
- `AddMediatRResponseCache` (فایل `Infrastructure/Cache/OutputCacheServiceExtensions.cs`) مستقل از حالت همیشه در `AddCaching`
  فراخوانی می‌شود؛ فقط provider آن بر اساس `Cache:UseRedis` عوض می‌شود (بخش «کش خروجی MediatR» پایین).
- در حالت Redis روشن، `IRateLimitService` به‌صورت اینترفیس ثبت نمی‌شود (فقط انواع عینی)؛ مصرف‌کننده‌هایی
  که با `GetRequiredService<IRateLimitService>()` آن را می‌گیرند (`RateLimitMiddleware` و فیلترهای
  Rate Limit) در این حالت به خطای حل وابستگی می‌رسند.

### `CacheOptions` (سکشن `Cache`)

| فیلد                       | پیش‌فرض کد | نقش                                                                           |
| -------------------------- | ---------- | ----------------------------------------------------------------------------- |
| `IsEnabled`                | `false`    | خاموش/روشن کلی کش (ملاک `RedisCacheHealthCheck`)                              |
| `UseRedis`                 | `false`    | انتخاب پیاده‌سازی‌ها (جدول بالا)                                              |
| `RedisConnectionString`    | رشته خالی  | جایگزین `ConnectionStrings:Redis`                                             |
| `DefaultExpirationMinutes` | ۳۰         | TTL پیش‌فرض `RedisCacheService`                                               |
| `KeyPrefix`                | `shop`     | پیشوند همه کلیدها (و بخشی از `InstanceName` کش خروجی: `{KeyPrefix}:mediatr:`) |

فیلدهای `ShortExpirationMinutes` و `LongExpirationMinutes` از `CacheOptions` حذف شده‌اند؛ TTL هر Query اکنون
مستقیماً روی اتریبیوت `[RequestOutputCache]` همان Query (`expirationInSeconds`) تعریف می‌شود.

کلیدهای `DefaultTtlMinutes` و `LockTtlSeconds` که در `appsettings.json` زیر `Cache` وجود دارند با هیچ
خاصیتی از `CacheOptions` هم‌نام نیستند و در کد اثری ندارند.

## پیاده‌سازی‌های `ICacheService`

| کلاس                         | مسیر                                   | رفتار                                                                                                                                        |
| ---------------------------- | -------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `RedisCacheService`          | `Infrastructure/Cache/Redis/Services/` | سریال‌سازی JSON با `camelCase`، کلید `{KeyPrefix}:{key}`، TTL پیش‌فرض ۳۰ دقیقه؛ خطاهای Redis فقط لاگ Warning می‌شوند و متد نتیجه عادی می‌دهد |
| `EncryptedRedisCacheService` | همان پوشه                              | Decorator رمزنگاری (پایین)                                                                                                                   |
| `InMemoryCacheService`       | `Infrastructure/Cache/Services/`       | روی `IMemoryCache`؛ TTL پیش‌فرض ۳۰ دقیقه                                                                                                     |

قرارداد `ICacheService` (در `Application/Cache/Contracts/`) یک key-value store ساده است و فقط چهار متد دارد:
`GetAsync`، `SetAsync`، `RemoveAsync` و `ExistsAsync`. متد `RemoveByPrefixAsync` و پیاده‌سازی `NoOpCacheService`
حذف شده‌اند. این سرویس دیگر برای کش نتیجه Queryها استفاده نمی‌شود (بخش «مصرف‌کننده‌های باقی‌مانده `ICacheService`»).

### رمزنگاری محتوای کش

`EncryptedRedisCacheService` فقط وقتی فعال می‌شود که `CacheEncryptionOptions.IsEnabled` باشد
(سکشن `Cache:Encryption`; `KeyBase64` الزاماً کلید ۳۲ بایتی، `KeyId` پیش‌فرض `v1`):

- الگوریتم AES-GCM با nonce ۱۲ بایتی و tag ۱۶ بایتی؛ payload شامل `Version`، `KeyId`، nonce/متن رمز/tag
  به‌صورت base64 و نسخه پشتیبانی‌شده `1` است.
- فقط نوع‌هایی رمز می‌شوند که در کلاس یا ویژگی‌هایشان `[Sensitive]` (`SharedKernel/Attributes/SensitiveAttribute.cs`)
  باشد؛ **در کد فعلی هیچ نوعی این اتریبیوت را ندارد**، پس در عمل هیچ مقداری رمز نمی‌شود.
- `RemoveAsync`/`ExistsAsync` بدون تغییر به لایه داخلی منتقل می‌شوند.

## Idempotency

| کلاس                      | کلید                        | TTL     | جزئیات                                                   |
| ------------------------- | --------------------------- | ------- | -------------------------------------------------------- |
| `RedisIdempotencyService` | `{KeyPrefix}:idem:{guid:N}` | ۲۴ ساعت | پاکت شامل نتیجه و زمان پردازش؛ ذخیره با `When.NotExists` |
| `CacheIdempotencyService` | `idempotency:{guid:N}`      | ۲۴ ساعت | روی همان `ICacheService` فعال                            |

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

## کش خروجی MediatR (Output Cache)

کش نتیجه Queryها با پکیج‌های `NexGen.MediatR.Extensions.Caching` (پروژه Application) و
`NexGen.MediatR.Extensions.Caching.Redis` و `NexGen.MediatR.Extensions.Caching.EntityFramework`
(پروژه Infrastructure) انجام می‌شود؛ هر سه نسخه `2.4.0`. سیستم قدیمی (`ICacheableQuery`، `CachingBehavior`،
`ICacheInvalidationService`/`CacheInvalidationService`، کلاس‌های `CacheKeys`، Handlerهای ابطال رویدادمحور و ابطال
دستی داخل Commandها) به‌طور کامل حذف شده است.

### انتخاب Query برای کش (opt-in)

کش فقط برای درخواست‌هایی فعال است که اتریبیوت `[RequestOutputCache]`
(فضای نام `NexGen.MediatR.Extensions.Caching.Attributes`) روی رکورد Query‌شان باشد. حدود ۶۰ Query این اتریبیوت را دارند:

```csharp
[RequestOutputCache(
    tags: [CacheTags.Category, CacheTags.Media],
    expirationInSeconds: 600)]
public record GetPublicCategoriesQuery(string? Search, int Page, int PageSize) : IPageQuery<CategoryDto>;
```

| پارامتر               | نقش                                                                              |
| --------------------- | -------------------------------------------------------------------------------- |
| `tags`                | فهرست تگ‌هایی که پاسخ به آن‌ها وابسته است؛ ابطال بر اساس همین تگ‌ها انجام می‌شود |
| `expirationInSeconds` | TTL پاسخ کش‌شده به ثانیه                                                         |

مثال‌ها: `GetProductCatalogQuery`، `GetProductQuery`، `GetCategoryTreeQuery`. Command هیچ‌وقت کش نمی‌شود و
Handlerهای Command دیگر با کش کاری ندارند.

### تگ‌ها (`CacheTags`)

تگ‌ها ثابت‌هایی در `Application/Cache/Contracts/CacheTags.cs` هستند. **مقدار هر تگ برابر نام نوع CLR موجودیت EF است**
(با `nameof`)، چون ابطال خودکار بر اساس نام موجودیت‌های تغییرکرده در `SaveChanges` انجام می‌شود:

| تگ                          | موجودیت           | تگ                         | موجودیت          |
| --------------------------- | ----------------- | -------------------------- | ---------------- |
| `CacheTags.Product`         | `Product`         | `CacheTags.Order`          | `Order`          |
| `CacheTags.ProductVariant`  | `ProductVariant`  | `CacheTags.OrderItem`      | `OrderItem`      |
| `CacheTags.VariantShipping` | `VariantShipping` | `CacheTags.OrderStatus`    | `OrderStatus`    |
| `CacheTags.Category`        | `Category`        | `CacheTags.ProductReview`  | `ProductReview`  |
| `CacheTags.Brand`           | `Brand`           | `CacheTags.AttributeType`  | `AttributeType`  |
| `CacheTags.Media`           | `Media`           | `CacheTags.AttributeValue` | `AttributeValue` |
| `CacheTags.Inventory`       | `Inventory`       | `CacheTags.Shipping`       | `Shipping`       |
| `CacheTags.Warehouse`       | `Warehouse`       | `CacheTags.PaymentMethod`  | `PaymentMethod`  |

تگ‌های دستی در `CacheTags.Manual` به هیچ موجودیت EF نگاشت نمی‌شوند و برای داده‌های «فقط TTL» هستند:

| تگ دستی                      | مقدار       | مصرف                                             |
| ---------------------------- | ----------- | ------------------------------------------------ |
| `CacheTags.Manual.Analytics` | `analytics` | گزارش‌های تحلیلی (Analytics)                     |
| `CacheTags.Manual.Location`  | `location`  | داده ثابت استان/شهر ([location.md](location.md)) |

قواعد تگ‌گذاری:

- Query باید **همه موجودیت‌هایی** را که پاسخش از آن‌ها ساخته می‌شود در `tags` فهرست کند؛ در غیر این صورت با
  تغییر آن موجودیت، پاسخ کهنه ابطال نمی‌شود و تا پایان TTL می‌ماند.
- تگ‌های `Manual` با تغییر داده ابطال خودکار نمی‌شوند و پاسخ‌ها فقط با TTL منقضی می‌شوند (یا با ابطال صریح از
  طریق `IRequestOutputCacheInvalidator`).
- **قاعده مهم:** درخواست کش‌شونده نباید به کاربر جاری وابسته باشد، مگر اینکه شناسه کاربر جزو payload خود درخواست باشد.

### ثبت در DI

`AddMediatRResponseCache` در `Infrastructure/Cache/OutputCacheServiceExtensions.cs` (فراخوانی‌شده از `AddCaching`)
`AddMediatROutputCache` پکیج را با provider زیر ثبت می‌کند:

| حالت                   | provider         | جزئیات                                                                                                                    |
| ---------------------- | ---------------- | ------------------------------------------------------------------------------------------------------------------------- |
| `Cache:UseRedis=true`  | `UseRedisCache`  | `ConnectionString` همان رشته اتصال Redis؛ `InstanceName = {Cache:KeyPrefix}:mediatr:`؛ `EnableDistributedEviction = true` |
| `Cache:UseRedis=false` | `UseMemoryCache` | کش حافظه‌ای درون‌فرایندی؛ بین نمونه‌های API مشترک نیست                                                                    |

رفتار `Behavior` پکیج در پلیپ‌لاین MediatR: پس از هشت Behavior خود پروژه ثبت می‌شود؛ یعنی درونی‌ترین لایه و درست کنار
Handler است (ترتیب کامل در [cqrs-features.md](../02-architecture/cqrs-features.md)).

### جریان ذخیره و ابطال

| گام                   | اتفاق                                                                                                                                                                                                              |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| ۱. درخواست Query      | کلید کش از نوع درخواست + **کل payload به‌صورت JSON** ساخته می‌شود؛ پس `Page`، `PageSize` و هر پارامتر دیگر بخشی از کلید است (کلیدهای قدیمی برای بعضی Queryهای صفحه‌بندی‌شده `Page`/`PageSize` را نادیده می‌گرفتند) |
| ۲. Cache Hit          | پاسخ بدون اجرای Handler برمی‌گردد و در پاسخ HTTP هدر `X-NexGen-Output-Cache: HIT` اضافه می‌شود                                                                                                                     |
| ۳. Cache Miss         | Handler اجرا می‌شود؛ فقط اگر `ServiceResult<T>.IsSuccess` باشد پاسخ با تگ‌ها و TTL ذخیره می‌شود. پاسخ‌های ناموفق (خطا، NotFound و ...) **هرگز کش نمی‌شوند**                                                        |
| ۴. تغییر داده         | Commandها به‌صورت عادی Aggregate را تغییر می‌دهند و `SaveChanges` می‌زنند                                                                                                                                          |
| ۵. ابطال خودکار       | `SaveChangesInterceptor` پکیج (`options.UseMediatROutputCacheAutoEvict(sp)`) نام نوع موجودیت‌های تغییرکرده را می‌خواند و هر پاسخ کش‌شده‌ای را که تگ‌هایش با آن‌ها بخواند حذف می‌کند                                |
| ۶. ابطال بین نمونه‌ها | در حالت Redis با `EnableDistributedEviction`، پیام ابطال از طریق Redis Pub/Sub به سایر نمونه‌های API هم می‌رسد                                                                                                     |

اتصال interceptor در `AddPersistence` (`Infrastructure/Common/DependencyInjection/InfrastructureServiceExtensions.cs`) با
متد `UseResponseCacheAutoEvict(sp)` روی `DbContextOptionsBuilder` انجام می‌شود که فقط یک wrapper روی
`UseMediatROutputCacheAutoEvict` است.

نکات:

- Pub/Sub ردیس **at-most-once** است؛ اگر پیام ابطالی به نمونه‌ای نرسد، پاسخ کهنه در آن نمونه تا پایان TTL می‌ماند.
  TTL کوتاه Queryها همین خطا را ترمیم می‌کند.
- ابطال فقط با `SaveChanges` از مسیر EF Core رخ می‌دهد؛ تغییرات دیتابیس بیرون از EF (مثل SQL مستقیم) پاسخ‌ها را باطل نمی‌کنند.
- نتایج جست‌وجو (`SearchProducts`، `FuzzySearch`، `GlobalSearch`، `GetSearchSuggestions`) چون Elasticsearch به‌صورت
  ناهمگام با Outbox به‌روز می‌شود، ممکن است تا حداکثر TTL (۶۰ یا ۱۲۰ ثانیه) عقب باشند.

### سریال‌سازی `ServiceResult<T>`

`ServiceResult<T>` سازنده خصوصی دارد و provider ردیس با Newtonsoft کار می‌کند. به همین دلیل
`ServiceResultJsonConverter` (`Infrastructure/Cache/Serialization/ServiceResultJsonConverter.cs`، مبتنی بر Newtonsoft)
از طریق `JsonConvert.DefaultSettings` ثبت می‌شود تا پاسخ‌ها از Redis قابل خواندن باشند. ثبت فقط یک‌بار انجام می‌شود
و `DefaultSettings` قبلی حفظ می‌شود.

### TTL نمونه‌ها

TTL Queryهای مهاجرت‌شده از سیستم قدیمی تغییری نکرده است:

| Query                                                                  | TTL      |
| ---------------------------------------------------------------------- | -------- |
| `GetInventoryReport`                                                   | ۵ دقیقه  |
| `GetDashboardStatistics`، `GetRevenueReport`                           | ۱۰ دقیقه |
| `GetCategoryPerformance`، `GetSalesChartData`، `GetTopSellingProducts` | ۱۵ دقیقه |
| `GetAllAttributeTypes`، `GetCategoryTree`، `GetAllWarehouses`          | ۱ ساعت   |
| `GetPublicBrands`، `GetPaymentMethods`، `GetShippings`                 | ۳۰ دقیقه |
| `GetProduct`، `GetOrderStatuses`                                       | ۱۰ دقیقه |
| `GetCities`، `GetStates`                                               | ۲۴ ساعت  |

نمونه Queryهای جدید: `GetProductCatalog` ۶۰ ثانیه، `GetProductDetails` ۱۲۰ ثانیه، `GetPublicCategories` ۶۰۰ ثانیه،
Queryهای موجودی (availability/status) ۱۵ تا ۳۰ ثانیه، محاسبه و پیشنهاد هزینه ارسال (`GetShippingQuotes`،
`CalculateShippingCost`، ...) ۳۰۰ ثانیه و Queryهای جست‌وجو ۶۰ ثانیه (به‌جز `GetSearchSuggestions` با ۱۲۰ ثانیه).

### چه چیزی عمداً کش نمی‌شود

- داده‌های وابسته به کاربر یا حساس: سبد خرید، کیف پول، علاقه‌مندی، اعلان‌ها، سشن‌ها، تیکت پشتیبانی، پروفایل/PII کاربر،
  سفارش‌های شخصی، `GetProductReviews`، `CanReviewProduct`، تراکنش‌های پرداخت و حسابرسی.
- کدهای تخفیف (Discounts)، چون به زمان و کاربر وابسته‌اند.
- Queryهای جزئیات فرم ویرایش ادمین (`GetAdminProduct*`، `GetInventory`) و دفتر/تراکنش‌های انبار (ledger).

### مصرف‌کننده‌های باقی‌مانده `ICacheService`

`ICacheService` فقط برای کاربردهای غیر-Query نگه داشته شده است:

| مصرف‌کننده                                            | کاربرد                                                           |
| ----------------------------------------------------- | ---------------------------------------------------------------- |
| `CacheIdempotencyService` / `RedisIdempotencyService` | ذخیره نتیجه Commandهای دارای `IdempotencyKey` (بخش Idempotency)  |
| `PaymentCallbackNonceService`                         | nonce بازگشت پرداخت ([payment-gateways.md](payment-gateways.md)) |
| `SessionActivityMiddleware`                           | ردیابی فعالیت سشن                                                |

`RedisCacheHealthCheck` و `CacheEncryptionOptions` بدون تغییر باقی مانده‌اند.

## Health Check

`RedisCacheHealthCheck` (`Infrastructure/Cache/Health/`) وقتی `Cache:IsEnabled` باشد اجرا می‌شود:

- `PingAsync` با اندازه‌گیری تأخیر + نوشتن/خواندن مقدار آزمایشی روی کلید `health:ping` (TTL ۵ ثانیه).
- تأخیر کل بیشتر از ۱۰۰ میلی‌ثانیه → `Degraded` با داده‌های `PingLatency`، `CheckLatency`،
  `ConnectedEndpoints` و `IsConnected`؛ ناهم‌خوانی نوشتن/خواندن → `Degraded`؛ خطای اتصال → `Unhealthy`.
- اگر `Cache:IsEnabled=false` باشد بدون لمس Redis، `Healthy` با پیام «Cache is disabled» برمی‌گردد.

## نکات و محدودیت‌ها

- خطاهای Redis در `RedisCacheService` بلعیده می‌شوند؛ قطعی کش هرگز درخواست را شکست نمی‌دهد اما هیچ
  متریک اختصاصی برای نرخ خطای کش وجود ندارد.
- `ICacheService` دیگر ابطال بر اساس پیشوند ندارد؛ ابطال پاسخ Queryها فقط از مسیر تگ‌ها و interceptor خودکار EF انجام می‌شود.
- Queryهای مربوط به اعلان‌ها و سشن‌ها عمداً در کش خروجی نیستند؛ مثلاً `NotificationQueryService` کش ندارد
  ([03-modules/notifications.md](../03-modules/notifications.md)).
- نگهداری کلیدهای DataProtection در Redis و رفتار Resilient آن در
  [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md) و
  [07-operations/security.md](../07-operations/security.md) آمده است.

## اسناد مرتبط

- قفل‌ها و Outbox در نمای معماری: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
- تنظیمات `Cache` و `Cache:Encryption`: [01-getting-started/configuration.md](../01-getting-started/configuration.md)
- Jobهای وابسته به قفل توزیع‌شده: [background-jobs.md](background-jobs.md)
- nonce بازگشت پرداخت روی Redis: [payment-gateways.md](payment-gateways.md)
