[بازگشت به فهرست یکپارچه‌ها](README.md)

# لایه فنی Elasticsearch

این سند لایه فنی جست‌وجو در `Infrastructure/Search` را پوشش می‌دهد: ساخت کلاینت و ثبت DI، سرویس‌های
ایندکس‌گذاری و Bulk، همگام‌سازی از دیتابیس، Handlerهای رویداد، Outbox اختصاصی، کلیدشکن و Health Checkها.
قابلیت‌های بیزینسی جست‌وجو (پرس‌وجوها، تحلیلگر فارسی، APIها) در [03-modules/search.md](../03-modules/search.md)
مستند شده‌اند؛ این سند آن جزئیات را تکرار نمی‌کند.

## سرویس‌ها و ثبت در DI

ثبت در `AddSearchServices` داخل `InfrastructureServiceExtensions.cs` انجام می‌شود و کاملاً دوحالته است:

| سرویس | حالت فعال (`Elasticsearch:IsEnabled=true`) | حالت غیرفعال |
|---|---|---|
| `ISearchService` | `ResilientElasticSearchService` | `NoOpSearchService` |
| `IElasticsearchIndexer` | `ElasticsearchIndexer` | `NoOpElasticsearchIndexer` |
| `IElasticIndexManager` | `ElasticIndexManager` | `NoOpElasticIndexManager` |
| `ISearchDatabaseSyncService` | `ElasticsearchDatabaseSyncService` | `NoOpSearchDatabaseSyncService` |
| کلاینت | `ElasticsearchClient` (Singleton) | ثبت نمی‌شود |

- کلاینت در DI مستقیماً با `ElasticsearchClientSettings(new Uri(options.Url)).DefaultIndex(...)` ساخته
  می‌شود؛ `ElasticClientFactory` که `Urls`، احراز هویت Basic، `RequestTimeout`، `MaximumRetries` و
  `DebugMode` را هم اعمال می‌کند، **فقط در پروژه تست‌ها** استفاده می‌شود.
- همه Optionsها (`ElasticsearchOptions` با سکشن `Elasticsearch`) با `ValidateOnStart` بسته می‌شوند؛
  فهرست فیلدها در [01-getting-started/configuration.md](../01-getting-started/configuration.md).

## مدیریت ایندکس

`ElasticIndexManager` ساخت/حذف سه ایندکس را با تعریف تحلیلگر فارسی انجام می‌دهد (جزئیات تحلیلگر در
[03-modules/search.md](../03-modules/search.md)). متدهای عمومی:

| متد | کار |
|---|---|
| `CreateProductIndexAsync` / `CreateCategoryIndexAsync` / `CreateBrandIndexAsync` | ساخت ایندکس با Mapping و تحلیلگر اختصاصی |
| `CreateAllIndicesAsync` | فراخوانی هر سه به‌ترتیب |
| `DeleteIndexAsync(indexName)` | حذف ایندکس |
| `IndexExistsAsync(indexName)` | بررسی وجود |
| `ReindexAsync(source, destination)` | `_reindex` سمت Elasticsearch |

نام ایندکس‌ها (`products_v1`, `categories_v1`, `brands_v1`) در چند کلاس هاردکد شده‌اند و
`ElasticsearchIndexOptions` بدون مصرف است (به نکات [03-modules/search.md](../03-modules/search.md)).

## ایندکس‌گذاری و Bulk

- `ElasticsearchIndexer.IndexDocumentAsync(entityType, entityId, document, changeType)`: نام ایندکس را از
  نوع موجودیت (`Product`/`Category`/`Brand`) تشخیص می‌دهد؛ `changeType = "Delete"` یعنی
  `DeleteAsync` (پاسخ `NotFound` هم موفق شمرده می‌شود) و بقیه `IndexAsync` سند JSON. شکست‌ها با لاگ
  حسابرسی ثبت و `false` برمی‌گردند — استثنا به بیرون پرتاب نمی‌شود.
- `ElasticBulkService` (`IElasticBulkService`): `BulkIndexProductsAsync`، `BulkIndexCategoriesAsync`،
  `BulkIndexBrandsAsync`، `BulkUpdateProductsAsync` و `BulkDeleteProductsAsync` روی همان سه ایندکس؛
  هر فراخوانی موفق با `ElasticsearchMetrics` شمارش می‌شود و شکست `DebugInformation` کامل را لاگ و
  استثنا را مجدداً پرتاب می‌کند.

## همگام‌سازی از دیتابیس

`ElasticsearchDatabaseSyncService` (`ISearchDatabaseSyncService`) با Dapper روی
`ISqlConnectionFactory` اسناد را مستقیم از PostgreSQL می‌خواند:

| متد | رفتار |
|---|---|
| `SyncProductAsync` / `SyncCategoryAsync` / `SyncBrandAsync` | یک سند با `JOIN` روی `Products`/`Brands`/`Categories`/`ProductVariants` (حداقل قیمت فروش و جمع موجودی تنوع‌ها) |
| `SyncAllProductsAsync` | دسته‌های ۵۰۰تایی با `LIMIT/OFFSET` و ارسال با Bulk |
| `SyncAllCategoriesAsync` / `SyncAllBrandsAsync` | یک کوئری کامل و ارسال Bulk |
| `FullSyncAsync` | ترتیب دسته → برند → محصول؛ `SyncAsync` همان را صدا می‌زند |

کلاس `ElasticsearchInitialSyncService` (دسته ۱۰۰۰تایی با سینتکس `OFFSET ... FETCH NEXT` سبک SQL Server)
**در DI ثبت و از هیچ مسیر اجرایی فراخوانی نمی‌شود**؛ مسیر واقعی همگام‌سازی کامل،
`SyncSearchDataCommand` در [03-modules/search.md](../03-modules/search.md) است.

## همگام‌سازی رویدادی و Outbox اختصاصی

نوشتن در صف با دو Handler در `Infrastructure/Search/EventHandlers/` انجام می‌شود:

| Handler | رویدادها | خروجی |
|---|---|---|
| `ElasticsearchEventHandler` | سه رویداد `ProductChangedEvent`/`CategoryChangedEvent`/`BrandChangedEvent` از `Application/Search/Events` | ردیف Outbox با نوع تغییر همان رویداد |
| `InventoryStockSearchSyncHandler` | `StockIncreasedEvent`, `StockReservedEvent`, `StockReservationReleasedEvent` | پیام Outbox با `changeType = "StockChanged"` و `discriminator` یکتا (`{inventoryId}:{عملیات}:{GUID}`)؛ `productId` با جست‌وجوی `ProductVariants` پیدا می‌شود |

`ElasticsearchOutboxMessage` (جدول `ElasticsearchOutboxMessages`) فیلدهای `IdempotencyKey`
(ترکیب `{type}:{id}:{changeType}` و در صورت وجود، discriminator)، `RetryCount`، `NextAttemptAt`،
`ProcessedAt`، `IsPoisoned` و متدهای `MarkProcessed`/`MarkFailed`/`MarkPoisoned` دارد.
`FailedIndexOperation` و جدول `FailedElasticOperations` صف مرده هستند و `ElasticDeadLetterQueue`
فقط متد `DequeueAsync` (برداشت `Pending`ها) را پیاده می‌کند. فرآیند برداشت هر دو صف،
`ElasticsearchOutboxJob` است که در [background-jobs.md](background-jobs.md) مستند شده.

## کلیدشکن و تاب‌آوری

`ResilientElasticSearchService` روی `ElasticsearchService` می‌نشیند و `ElasticsearchCircuitBreaker`
(ماشین حالت `Closed`/`Open`/`HalfOpen`) را اعمال می‌کند:

- آستانه‌ها با خواندن مستقیم IConfiguration: `Elasticsearch:CircuitBreaker:FailureThreshold` (پیش‌فرض ۵)
  و `Elasticsearch:CircuitBreaker:BreakDurationSeconds` (پیش‌فرض ۶۰).
- باز و بسته شدن مدار با لاگ حسابرسی ثبت می‌شود؛ در حالت Open متدهای ایندکس/جست‌وجو نتیجه خالی
  می‌دهند و خطاها شمارش می‌شوند.
- `DeleteProductAsync`، `SearchProductsAsync` و `GetIndexStatsAsync` از کلیدشکن عبور نمی‌کنند و
  مستقیم به سرویس داخلی می‌روند.

`ElasticsearchMetrics` یک `Meter` با نام `Elasticsearch` می‌سازد و شمارنده‌هایی مانند
`elasticsearch.search.requests`، `elasticsearch.bulk.success`، `elasticsearch.errors` و
هیستوگرام‌های مدت `elasticsearch.search.duration`/`elasticsearch.index.duration`/
`elasticsearch.bulk.duration` را منتشر می‌کند؛ مصرف‌کننده آن فقط `ElasticBulkService` است.

## Health Checkها

| نام | کلاس | آستانه‌ها |
|---|---|---|
| `elasticsearch` | `ElasticsearchHealthCheck` | `PingAsync` سمت کلاینت؛ شکست → Unhealthy |
| `elasticsearch-index` | `ElasticsearchIndexHealthCheck` | بررسی وجود سه ایندکس: نبود همه → Unhealthy، نبود برخی → Degraded |
| `elasticsearch-dlq` | `ElasticsearchDLQHealthCheck` | روی `FailedElasticOperations`: بیش از ۱۰۰ ردیف `Failed` → Unhealthy، بیش از ۱۰۰۰ `Pending` → Degraded |

ثبت مشروط به `IsEnabled` و Endpointهای `/health/*` در
[02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md) آمده است.

## نکات و محدودیت‌ها

- **گراف DI در حالت فعال ناقص است**: `ResilientElasticSearchService` به کلاس‌های عینی
  `ElasticsearchService` و `ElasticsearchCircuitBreaker` و `ElasticsearchDatabaseSyncService` به
  `IElasticBulkService` (و آن هم به `ElasticsearchMetrics`) وابسته است، اما هیچ‌یک در
  `InfrastructureServiceExtensions` ثبت نشده‌اند؛ در کد فعلی با `IsEnabled=true`، حل وابستگی این
  سرویس‌ها در زمان اجرا با خطا مواجه می‌شود.
- کلاینت ساخته‌شده در DI نه احراز هویت می‌گیرد و نه Timeout/Retry مشخصی ست می‌کند؛ فیلدهای
  `Username`/`Password`/`TimeoutSeconds`/`MaxRetries`/`DebugMode` فقط در `ElasticClientFactory`
  (بی‌مصرف در تولید) اثر دارند.
- مقادیر پیشنهادی سکشن `Elasticsearch` در appsettings مانند `BulkOperations`, `CircuitBreaker`,
  `DeadLetterQueue`, `Sync` بیشتر از راه خواندن مستقیم IConfiguration در Job و کلیدشکن استفاده
  می‌شوند تا کلاس Options؛ `ElasticsearchIndexOptions` و `ElasticsearchMetricsOptions` کلاس‌های
  بدون مصرف‌اند.
- سینتکس SQL دو سرویس همگام‌سازی ناسازگار است (`false`/`LIMIT` در سرویس ثبت‌شده در برابر
  `0`/`FETCH NEXT` در سرویس بی‌مصرف)؛ نشانه‌ای از اینکه کد دوم کامل‌نشده است.

## اسناد مرتبط

- قابلیت‌های جست‌وجو، تحلیلگر فارسی و API: [03-modules/search.md](../03-modules/search.md)
- Jobهای `ElasticsearchOutboxJob`/`ElasticsearchSyncJob`: [background-jobs.md](background-jobs.md)
- Outbox اصلی و Health Checkها: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
- تنظیمات Elasticsearch: [01-getting-started/configuration.md](../01-getting-started/configuration.md)
