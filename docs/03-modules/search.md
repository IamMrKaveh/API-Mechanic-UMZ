[بازگشت به فهرست ماژول‌ها](README.md)

# جست‌وجو (Search)

جست‌وجو روی Elasticsearch انجام می‌شود: سه ایندکس برای محصول، دسته و برند، تحلیلگر فارسی اختصاصی،
جست‌وجوی فازی و پیشنهاددهی خودکار. این ماژول Domain ندارد؛ Application آن دو Command و پنج Query و API آن
۷ اکشن در دو کنترلر عمومی و مدیریتی است. تأثیر موجودی بر ایندکس در [inventory.md](inventory.md) آمده است.

## ساختار

`Domain/Search` وجود ندارد؛ منطق در `Application/Search`، `Infrastructure/Search` و `Presentation/Search`
توزیع شده است. ایندکس‌ها: `products_v1`, `categories_v1`, `brands_v1` — این نام‌ها در کد به‌صورت رشته
هاردکد شده‌اند (`ElasticIndexManager`, `ElasticSearchService`, `ElasticsearchIndexer`, `RecreateSearchIndicesHandler`),
هرچند کلاس `ElasticsearchIndexOptions` مقادیر مشابهی را تعریف کرده و در جای دیگری استفاده نمی‌شود.

## Application (`Application/Search`)

**۲ Command:** `RecreateSearchIndicesCommand` (حذف سه ایندکس و ساخت مجدد)، `SyncSearchDataCommand`
(همگام‌سازی کامل از دیتابیس با `ISearchDatabaseSyncService.FullSyncAsync`).

**۵ Query:** `SearchProducts`, `GlobalSearch`, `FuzzySearch`, `GetSearchSuggestions`, `GetSearchIndexStats`.

چهار Validator وجود دارد (برای همه به‌جز `GetSearchIndexStats`).
اسناد ایندکس در `Features/Shared/SearchDtos.cs` تعریف شده‌اند: `ProductSearchDocument` (۲۴ فیلد شامل
قیمت، موجودی، تعداد موجودی، میانگین امتیاز، تعداد نظر، تعداد فروش و برچسب‌ها)، `CategorySearchDocument`,
`BrandSearchDocument` و رکورد صف مرده `FailedElasticOperation`.

**رخدادهای تغییر موجودیت** (سه رکورد در `Application/Search/Events/`): `ProductChangedEvent`,
`CategoryChangedEvent`, `BrandChangedEvent` که هر سه `IEntityChangeEvent` (`EntityId`, `EntityType`,
`ChangeType`) را پیاده می‌کنند. **هیچ ناشری برای این سه رکورد در کد اجرایی وجود ندارد**؛ فقط Handler و
تست‌ها به آن‌ها ارجاع می‌دهند (جزئیات در بخش «نکات»).

## API

جمع: **۲ کنترلر و ۷ اکشن**. قراردادهای عمومی در [05-api/conventions.md](../05-api/conventions.md).

`SearchController` — `[Route("api/v{version:apiVersion}/search")]` — بدون احراز هویت؛ ۴ اکشن:

| متد | مسیر | اکشن |
|---|---|---|
| GET | `products` | `SearchProducts` |
| GET | `global` | `GlobalSearch` |
| GET | `suggestions` | `GetSuggestions` |
| GET | `products/fuzzy` | `SearchWithFuzzy` |

`AdminSearchController` — `[Route("api/v{version:apiVersion}/admin/search")]` — `[Authorize(Roles = "Admin")]`؛ ۳ اکشن:
`POST sync` → `SyncAllData`، `POST indices` → `RecreateIndices`، `GET stats` → `GetIndexStats`.

## زیرساخت

- کلاینت رسمی `Elastic.Clients.Elasticsearch` از طریق `ElasticClientFactory` ساخته می‌شود
  (آدرس اول `Urls`، `RequestTimeout`، `MaximumRetries`، احراز هویت Basic اختیاری). تنظیمات
  `ElasticsearchOptions` (بخش `Elasticsearch`): `IsEnabled`, `Url` (پیش‌فرض `http://localhost:9200`),
  `DefaultIndex`, `EnableBackgroundSync`, `MaxRetryCount`, `RequestTimeoutSeconds`, `Urls`,
  `TimeoutSeconds` (۳۰), `MaxRetries` (۳), `Username`, `Password`, `DebugMode`.
- **تحلیلگر فارسی** در `ElasticIndexManager`: فیلتر نویسه `persian_char_filter` (تبدیل ارقام فارسی،
  `ي→ی`، `ك→ک`، حذف نیم‌فاصله و اعراب)، فیلترهای `persian_stop` (کلمات ایست فارسی) و
  `persian_stemmer` (زبان persian)، فیلتر `edge_ngram_filter` (۲ تا ۱۵) و تحلیلگرهای `persian_advanced`,
  `persian_autocomplete`, `persian_autocomplete_search` به‌همراه نرمال‌ساز `persian_normalizer`.
  شارد/ریپلیکا از `Elasticsearch:NumberOfShards`/`NumberOfReplicas` (پیش‌فرض ۱ و ۰)، بازه تازه‌سازی ۵۰۰۰
  میلی‌ثانیه و `MaxResultWindow` ده‌هزار.
- **قابلیت‌های پرس‌وجو**: `MultiMatch` با `Fuzziness.AUTO` روی نام/توضیحات/نام دسته/نام برند، فیلترهای
  `categoryId`, `brandId`, `inStock` و بازه قیمت، و `MatchPhrasePrefix` روی نام برای پیشنهادها.
  **Aggregation برای facet، highlighting و اعمال `SortBy` در کد وجود ندارد**؛ `SearchGlobalAsync` فقط
  محصولات را برمی‌گرداند و فهرست دسته/برند خالی می‌ماند.
- **Outbox اختصاصی جست‌وجو**: موجودیت `ElasticsearchOutboxMessage` (جدول `ElasticsearchOutboxMessages`
  با `IdempotencyKey`, `RetryCount`, `NextAttemptAt`, `IsPoisoned`) و صف مرده در جدول
  `FailedElasticOperations`. پردازش با `ElasticsearchOutboxJob` (فاصله پیش‌فرض ۶۰ ثانیه،
  `MaxRetries` پیش‌فرض ۵، دسته پیش‌فرض ۱۰۰ و backoff نمایی تا ۳۰۰ ثانیه) و همگام‌سازی دوره‌ای با
  `ElasticsearchSyncJob` (ساعتی، وقتی `EnableBackgroundSync` روشن باشد، قفل `jobs:elasticsearch-sync`).
- **تاب‌آوری**: `ResilientElasticSearchService` روی `ElasticsearchService` با کلیدشکن
  (`Elasticsearch:CircuitBreaker:FailureThreshold=5`, `BreakDurationSeconds=60`) و متریک‌ها؛
  Health Checkهای `ElasticsearchHealthCheck`, `ElasticsearchIndexHealthCheck` و `ElasticsearchDLQHealthCheck`.
- **رفتار در نبود Elasticsearch**: اگر `IsEnabled=false` باشد سرویس‌های NoOp ثبت می‌شوند
  (`NoOpSearchService` خروجی خالی، `NoOpElasticsearchIndexer`, `NoOpElasticIndexManager`,
  `NoOpSearchDatabaseSyncService`)؛ در حالت فعال، خطاهای ES با کلیدشکن بلعیده و نتیجه خالی برگردانده می‌شود.
  **هیچ fallback به دیتابیس برای Queryهای جست‌وجو وجود ندارد**؛ فقط مسیر دیتابیس→ES
  (`ElasticsearchDatabaseSyncService`, `ElasticsearchInitialSyncService`) پیاده شده است.

## نکات و محدودیت‌ها

- **تنها مسیر فعال همگام‌سازی رویدادی، تغییرات موجودی است**: `InventoryStockSearchSyncHandler` به
  `StockIncreasedEvent`, `StockReservedEvent` و `StockReservationReleasedEvent` گوش می‌دهد و برای نوع
  موجودیت `Product` با نوع تغییر `StockChanged` پیام Outbox می‌نویسد. رکوردهای
  `ProductChangedEvent`/`CategoryChangedEvent`/`BrandChangedEvent` ناشر ندارند، بنابراین تغییر نام یا
  قیمت محصول از مسیر رویداد به ایندکس نمی‌رسد و به همگام‌سازی دوره‌ای/دستی وابسته است.
- ایندکس `products_v1` شامل فیلدهایی مثل میانگین امتیاز، تعداد نظر و تعداد فروش است، اما Handler
  اختصاصی برای رویدادهای Review یا Order در این ماژول ثبت نشده؛ به‌روزرسانی این مقادیر تنها با
  همگام‌سازی کامل انجام می‌شود.
- در نبود Elasticsearch، جست‌وجوی فروشگاه نتیجه خالی می‌دهد و جایگزینی در دیتابیس ندارد؛ این رفتار
  برای محیط‌هایی که ES خاموش است باید در نظر گرفته شود.
- کلاس‌های `SearchConfiguration` (بوست فیلدها، `EnableFuzzySearch`, `EnableSuggestions`,
  `SuggestionCount`)، `IndexConfiguration`, `AnalyzerConfiguration` و `ElasticsearchMetricsOptions`
  تعریف شده‌اند اما در جای دیگری ارجاع نمی‌شوند؛ پیکربندی مؤثر همان مقادیر هاردکد و `ElasticsearchOptions` است.
- هیچ Aggregation یا فیلتر فاست (facet) در کد ساخته نمی‌شود؛ شمارش‌های دسته/برند در پاسخ جست‌وجو
  محاسبه نمی‌شوند.

## اسناد مرتبط

- محصول و رویدادهای آن: [products.md](products.md) — موجودی و رویدادهای انبار: [inventory.md](inventory.md)
- دسته و برند: [categories.md](categories.md)، [brands.md](brands.md)
- Outbox، Jobها و Health Checkها: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
- تنظیمات Elasticsearch و FeatureManagement: [01-getting-started/configuration.md](../01-getting-started/configuration.md)
