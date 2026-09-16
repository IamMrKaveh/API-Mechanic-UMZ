[بازگشت به فهرست ماژول‌ها](README.md)

# تنوع محصول و SKU (Variants)

هر محصول فروش‌پذیر از طریق `ProductVariant` تعریف می‌شود: SKU، قیمت فروش و اصلی، اتصال به
ویژگی‌ها (مثل رنگ) و پیکربندی ارسال هر واریانت. این سند مرز قیمت/SKU/ویژگی‌های واریانت است؛
موجودی و انبار در [inventory.md](inventory.md) و خود محصول در [products.md](products.md) مستند شده‌اند.

## دامنه (`Domain/Variant`)

Aggregate `ProductVariant : AggregateRoot<VariantId>, ISoftDeletable` با `ProductId`, `Sku`,
`OriginalPrice`, `SellingPrice` (از نوع `Money` با ارز پیش‌فرض `IRT`), `IsActive` و دو مجموعه
`Attributes` و `Shippings`. ویژگی‌های محاسبه‌شده `IsDiscounted` و `DiscountPercentage` از نسبت
قیمت اصلی به فروش به‌دست می‌آیند.

| متد | قاعده/اثر |
|---|---|
| `Create` | قیمت فروش باید مثبت باشد؛ قیمت اصلی کمتر از فروش و ناهماهنگی ارز ممنوع؛ صدور `VariantCreatedEvent` |
| `ChangePrice` | الزام `EnsureActive` (واریانت غیرفعال/حذف‌شده با `InvalidVariantOperationException` رد می‌شود)؛ صدور `ProductVariantPriceChangedEvent` فقط در صورت تغییر واقعی |
| `ChangeSku` | الزام فعال‌بودن؛ در تساوی بی‌اثر؛ **یکتایی SKU در دامنه بررسی نمی‌شود** (در Repository و دیتابیس) |
| `SetAttributes` | حذف تکراری بر اساس `ValueId`، همگام‌سازی اختلافی `VariantAttribute`؛ صدور `VariantAttributeSetEvent` |
| `SetShippingMethods` | ضریب ارسال باید مثبت باشد؛ همگام‌سازی `VariantShipping`؛ صدور `VariantShippingSetEvent` |
| `Activate` / `Deactivate` / `Remove` | فعال‌سازی، غیرفعال‌سازی و حذف نرم با صدور `VariantRemovedEvent` |

Entityها: `VariantAttribute` (اتصال واریانت به `AttributeType`/`AttributeValue` با `DisplayValue`) و
`VariantShipping` (اتصال واریانت به روش ارسال با وزن و ابعاد و ضریب؛ ابعاد منفی ممنوع).

ValueObjectها: `Sku` (حروف بزرگ، حداکثر ۱۰۰ کاراکتر، فقط `[A-Z0-9\-_\.]`)،
`AttributeAssignment` (Type/Value/DisplayValue) و شناسه‌های strongly-typed.

Domain Eventها (۵): `VariantCreatedEvent`, `ProductVariantPriceChangedEvent`, `VariantAttributeSetEvent`,
`VariantShippingSetEvent`, `VariantRemovedEvent`.

Exception: `InvalidVariantOperationException` با کد `INVALID_VARIANT_OPERATION` برای هر تغییر
روی واریانت غیرفعال یا حذف‌شده. پوشه `Rules/` خالی است.

## Application (`Application/Variant`)

**۴ Command:** `AddVariant`, `UpdateVariant`, `RemoveVariant`, `UpdateVariantShipping` — هر چهار
Validator دارند. (پوشه‌های `BulkDeleteVariants`، `BulkUpdateVariantStatus` و `CloneVariant` در کد
وجود دارند اما خالی‌اند.)

**۲ Query:** `GetVariants`, `GetVariantShipping`. پوشه `EventHandlers` خالی است.

قواعد Handlerها:

- `AddVariantHandler` تولید خودکار SKU را در نبود آن انجام می‌دهد
  (`Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()`)، وجود مقادیر ویژگی و «حداکثر یک مقدار
  برای هر نوع ویژگی» را می‌سنجد، ترکیب تکراری ویژگی‌ها را با `ExistsByAttributeCombinationAsync` و
  SKU تکراری را با پاسخ ۴۰۹ رد می‌کند، شناسه‌های روش ارسال را اعتبارسنجی می‌کند و برای هر واریانت
  یک Aggregate `Inventory` با آستانه کمبود ۵ می‌سازد (به [inventory.md](inventory.md)).
- `UpdateVariantHandler` تعلق واریانت به محصول را الزامی می‌کند و تغییر SKU را از نظر یکتایی می‌سنجد.
- `UpdateVariantShippingHandler` شناسه‌های ارسال را با `IShippingRepository` اعتبارسنجی، ابعاد موجود
  را حفظ و لاگ حسابرسی می‌نویسد.

## API

جمع: **۲ کنترلر و ۷ اکشن**، همه مدیریتی؛ کنترلر عمومی برای واریانت وجود ندارد.
قراردادهای عمومی در [05-api/conventions.md](../05-api/conventions.md).

`AdminVariantController` — `[Route("api/v{version:apiVersion}/admin/products/variants")]` — `[Authorize(Roles = "Admin")]`:

| متد | مسیر | اکشن | اشاره |
|---|---|---|---|
| POST | — | `Add` | با `productId` در Query |
| POST | `{variantId:guid}/stock` | `AddStock` | Command ماژول Inventory را می‌فرستد |
| PUT | `{variantId:guid}` | `Update` | با `productId` در Query |
| DELETE | `{variantId:guid}` | `Delete` | `RemoveVariantCommand` |
| DELETE | `{variantId:guid}/stock` | `RemoveStock` | Command ماژول Inventory |

`AdminVariantShippingController` — `[Route("api/v{version:apiVersion}/admin/variants/shipping")]` — `[Authorize(Roles = "Admin")]`:
`GET {variantId:guid}` → `GetVariantShipping`، `PUT {variantId:guid}` → `UpdateVariantShipping`.

## زیرساخت

- `VariantConfiguration` جدول `ProductVariants` را با ایندکس یگانه `Sku`، `Money` به‌صورت نوع مالک‌شده
  (`decimal(18,2)` با ستون جدا برای ارز) و حذف آبشاری `Attributes`/`Shippings` می‌سازد؛
  فیلتر سراسری حذف نرم ندارد و `!IsDeleted` دستی اعمال می‌شود.
- `VariantAttributeConfiguration` دو ایندکس یگانه ترکیبی روی `(VariantId, ValueId)` و
  `(VariantId, AttributeTypeId)` دارد که ترکیب تکراری ویژگی را در سطح دیتابیس هم می‌بندد؛
  فیلتر `!e.AttributeType.IsDeleted` اعمال می‌شود.
- یکتایی SKU در دو لایه تضمین می‌شود: `VariantRepository.ExistsBySkuAsync` (به‌جز حذف‌شده‌ها) و
  ایندکس یگانه؛ خطای یکتایی PostgreSQL در Handler به ۴۰۹ ترجمه می‌شود.
- `GetVariantAvailability` عمومی و ورودی‌های موجودی از ماژول Inventory می‌آیند — مرز ماژول‌ها:
  Endpointهای `/stock` همین کنترلر، `AddStockCommand`/`RemoveStockCommand` ماژول Inventory را اجرا می‌کنند.

## نکات و محدودیت‌ها

- حذف واریانت نرم است (`Remove`) اما `Product.Variants` حذف آبشاری دارد؛ تفاوت رفتار بین حذف دامنه و
  حذف دیتابیس را باید در مایگریشن‌ها در نظر گرفت.
- یکتایی SKU در Domain نیست؛ اگر واریانتی در برنامه دیگری SKU تکراری ثبت کند، فقط قید دیتابیس جلوی
  آن را می‌گیرد.
- `PriceChangedEvent` در `Domain/Product` روایت‌شده است اما در کد صادر نمی‌شود؛ رویداد واقعی تغییر قیمت
  `ProductVariantPriceChangedEvent` است و Handler ابطال کش محصول به آن گوش می‌دهد
  (به [products.md](products.md)).
- تغییر قیمت از مسیر Endpointهای `prices/bulk` و `{id}/details` کنترلر محصول نیز انجام می‌شود؛
  هر دو مسیر به همین Aggregate می‌رسند.

## اسناد مرتبط

- محصول و اسلاگ: [products.md](products.md)
- موجودی، رزرو و دفتر انبار: [inventory.md](inventory.md)
- ویژگی‌ها: [attributes.md](attributes.md) — روش‌های ارسال و ضریب: [shipping.md](shipping.md)
