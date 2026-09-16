[بازگشت به فهرست ماژول‌ها](README.md)

# محصولات (Products)

ماژول `Product` ریشه کاتالوگ است: نام، اسلاگ، توضیحات، وضعیت فعال/ویژه و آمار امتیاز هر محصول.
Aggregate آن هفت Domain Event دارد و Application آن نُه Command و هفت Query را در دو کنترلر عمومی و
مدیریتی ارائه می‌کند. قیمت، SKU و موجودی به‌ترتیب در [variants.md](variants.md) و [inventory.md](inventory.md)
مستند شده‌اند و این سند فقط سطح «محصول» را پوشش می‌دهد.

## دامنه (`Domain/Product`)

`Product : AggregateRoot<ProductId>, ISoftDeletable` با فیلدهای `Name`, `Slug`, `Description`,
`IsActive`, `IsFeatured`, `AvgRating`, `ReviewCount` و نشانه‌های حذف نرم.

| متد | قاعده/اثر |
|---|---|
| `Create` | محصول فعال می‌سازد و `ProductCreatedEvent` صادر می‌کند |
| `UpdateDetails` | تغییر نام/اسلاگ/توضیحات؛ صدور `ProductUpdatedEvent` |
| `ChangeBrand` / `ChangeCategory` | تغییر برند یا دسته؛ در صورت یکسان‌بودن بی‌اثر است |
| `Activate` / `Deactivate` | تغییر وضعیت با idempotency؛ صدور رویداد متناظر |
| `MarkAsFeatured` / `UnmarkAsFeatured` | علامت‌گذاری ویژه؛ **رویدادی صادر نمی‌شود** |
| `MarkAsDeleted` | حذف نرم + غیرفعال‌سازی؛ رویداد اختصاصی حذف وجود ندارد و `ProductDeactivatedEvent` صادر می‌شود |
| `Restore` | بازگردانی از حذف نرم |
| `RecalculateReviewStats` | میانگین امتیاز باید بین ۰ تا ۵ و تعداد منفی نباشد؛ گرد کردن دو رقم اعشار |

ValueObjectها: `ProductId`, `ProductName` (۲ تا ۱۰۰ کاراکتر)، `ProductSlug` (بر پایه `Slug` در
`SharedKernel` با تولید خودکار از نام). پوشه `Rules/` خالی است و قاعده‌ای به‌صورت کلاس جدا وجود ندارد.

Exceptionها: `InvalidPriceException` با کد `INVALID_PRICE` (مشترک با ماژول Variant) و
`ProductNotAvailableException` با کد `PRODUCT_NOT_AVAILABLE` و سازنده‌های `Deleted/Inactive/VariantInactive/OutOfStock`.

Domain Eventها (۷): `ProductCreatedEvent`, `ProductUpdatedEvent`, `ProductActivatedEvent`,
`ProductDeactivatedEvent`, `ProductBrandChangedEvent`, `ProductCategoryChangedEvent`, `PriceChangedEvent`.

## Application (`Application/Product`)

**۹ Command:** `CreateProduct`, `UpdateProduct`, `UpdateProductDetails`, `ChangePrice`, `BulkUpdatePrices`,
`ActivateProduct`, `DeactivateProduct`, `DeleteProduct`, `RestoreProduct`. هشت Validator وجود دارد
(همه به‌جز `RestoreProduct`).

**۷ Query:** `GetProducts`, `GetProduct`, `GetProductCatalog`, `GetProductDetails`, `GetAdminProducts`,
`GetAdminProduct`, `GetAdminProductDetail`.

نکات جریان‌ها:

- `CreateProductHandler` اسلاگ را با `ProductSlug.GenerateFrom(name)` می‌سازد و در صورت تکرار اسلاگ
  پاسخ Conflict (۴۰۹) می‌دهد.
- `UpdateProductHandler` چند متد دامنه را ترکیب می‌کند (`UpdateDetails`، `ChangeBrand`، `ChangeCategory`،
  `Activate`/`Deactivate`) و همزمانی خوش‌بینانه را با `RowVersion.FromBase64RowVersion()` اعمال می‌کند.
- `ChangePriceCommand` و `BulkUpdatePricesCommand` در واقع **روی واریانت‌ها** عمل می‌کنند و از
  `IVariantRepository` استفاده می‌کنند؛ جزئیات قیمت‌گذاری در [variants.md](variants.md) است.
- `DeleteProductHandler` به‌جای `MarkAsDeleted`، متد `Deactivate()` را صدا می‌زند؛ یعنی حذف نرم کامل
  از این مسیر انجام نمی‌شود و فقط وضعیت فعال/غیرفعال تغییر می‌کند.
- `GetProductQuery` از `ICacheableQuery` با کلید `product:{id}` و انقضای ۱۰ دقیقه استفاده می‌کند.
  همه Handlerهای Query به `IProductQueryService` واگذار می‌کنند.
- پوشه `Application/Product/EventHandlers` خالی است؛ واکنش به رویدادها در لایه زیرساخت انجام می‌شود.

## API

قراردادهای مسیر و پاکت پاسخ در [05-api/conventions.md](../05-api/conventions.md) است؛ زیر فقط
مسیرهای همین ماژول می‌آید. جمع: **۲ کنترلر و ۱۶ اکشن**.

`ProductsController` — `[Route("api/v{version:apiVersion}/products")]` — `[AllowAnonymous]`؛ ۵ اکشن:

| متد | مسیر | اکشن |
|---|---|---|
| GET | — | `GetProducts` |
| GET | `{id:guid}` | `GetProduct` |
| GET | `catalog` | `GetCatalog` |
| GET | `discounted` | `GetDiscountedProducts` |
| GET | `{id:guid}/details` | `GetProductDetails` |

`AdminProductsController` — `[Route("api/v{version:apiVersion}/admin/products")]` — `[Authorize(Roles = "Admin")]`؛ ۱۱ اکشن:

| متد | مسیر | اکشن |
|---|---|---|
| GET | — | `GetProducts` |
| GET | `{id:guid}` | `GetProduct` |
| GET | `{id:guid}/details` | `GetProductDetail` |
| POST | — | `CreateProduct` |
| PUT | `{id:guid}` | `UpdateProduct` |
| PATCH | `prices/bulk` | `BulkUpdatePrices` |
| PATCH | `{id:guid}/details` | `UpdateProductDetails` |
| DELETE | `{id:guid}` | `DeleteProduct` |
| PATCH | `{id:guid}/activate` | `ActivateProduct` |
| PATCH | `{id:guid}/deactivate` | `DeactivateProduct` |
| PATCH | `{id:guid}/restore` | `RestoreProduct` |

## زیرساخت

- `ProductConfiguration` جدول `Products` را با ستون سیستمی PostgreSQL یعنی `xmin` به‌عنوان
  `IsConcurrencyToken` می‌سازد، ایندکس یگانه روی `Slug` دارد و کلیدهای خارجی برند/دسته را با
  `DeleteBehavior.Restrict` نگه می‌دارد؛ واریانت‌ها آبشاری حذف می‌شوند. فیلتر سراسری حذف نرم وجود ندارد
  و فیلتر `!IsDeleted` در Repository/Query به‌صورت دستی اعمال می‌شود.
- همگام‌سازی جست‌وجو از مسیر Outbox اختصاصی Elasticsearch انجام می‌شود (ایندکس `products_v1`)؛
  جزئیات در [search.md](search.md).
- `ProductCacheInvalidationHandler` در `Infrastructure/Cache/EventHandlers` به رویدادهای
  `ProductUpdatedEvent`, `PriceChangedEvent`, `ProductActivatedEvent` و `ProductDeactivatedEvent`
  واکنش می‌دهد و کش محصول را باطل می‌کند؛ سازوکار کش در
  [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md) توضیح داده شده است.

## نکات و محدودیت‌ها

- `PriceChangedEvent` تعریف شده اما **هیچ‌جا صادر نمی‌شود**؛ مسیر واقعی تغییر قیمت
  `ProductVariantPriceChangedEvent` در ماژول Variant است. یعنی `ProductCacheInvalidationHandler`
  برای این رویداد هرگز فراخوانی نمی‌شود.
- `MarkAsFeatured` رویداد ندارد؛ تغییر وضعیت «ویژه» در هیچ رویدادی منتشر نمی‌شود.
- `DeleteProductCommand` عملاً غیرفعال‌سازی است (نه حذف نرم)، در حالی که `RestoreProduct` روی
  حذف نرم کار می‌کند؛ بنابراین چرخه حذف/بازگردانی از Endpoint مدیریتی کاملاً متقارن نیست.
- مرز ماژول: موجودی و انبار در [inventory.md](inventory.md)، SKU و قیمت در [variants.md](variants.md)،
  ویژگی‌ها در [attributes.md](attributes.md) و امتیاز/نظر در [reviews.md](reviews.md) مستند شده‌اند.
- پوشه `Rules/` در `Domain/Product` خالی است؛ اعتبارسنجی‌ها در ValueObjectها و Validatorهای
  Application متمرکز شده‌اند.

## اسناد مرتبط

- تنوع محصول، SKU، قیمت و اتصال ویژگی/ارسال: [variants.md](variants.md)
- ویژگی‌ها و مقادیر ویژگی: [attributes.md](attributes.md)
- دسته‌بندی و برند: [categories.md](categories.md)، [brands.md](brands.md)
- موجودی و دفتر انبار: [inventory.md](inventory.md)
- ایندکس جست‌وجو و Outbox اختصاصی: [search.md](search.md)
