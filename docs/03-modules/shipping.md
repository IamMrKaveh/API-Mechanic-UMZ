[بازگشت به فهرست ماژول‌ها](README.md)

# روش‌های ارسال (Shipping)

`Shipping` روش‌های ارسال را با هزینه پایه، آستانه ارسال رایگان، بازه مبلغ سفارش و بازه زمان تحویل
تعریف می‌کند و محاسبه هزینه را در اختیار دارد. Application آن پنج Command و شش Query دارد و ۱۱ اکشن در
سه کنترلر ارائه می‌کند. اعتبارسنجی ارسال در ثبت سفارش ([orders.md](orders.md)) و اتصال ارسال به واریانت
([variants.md](variants.md)) جداگانه مستند شده‌اند.

## دامنه (`Domain/Shipping`)

`Shipping : AggregateRoot<ShippingId>, IActivatable, IAuditable, ISoftDeletable` با `Name`, `Description?`,
`BaseCost` (از نوع `Money`), `EstimatedDeliveryTime?`, `DeliveryTime`, `IsActive` (پیش‌فرض روشن),
`SortOrder`, `IsDefault`, `OrderRange`, `MaxWeight?`, `FreeShipping` و نشانه‌های حذف نرم.
ویژگی `Cost` همان `BaseCost` است.

| متد | قاعده/اثر |
|---|---|
| `Create` | ساخت با بازه نامحدود مبلغ، ارسال رایگان غیرفعال و `IsDefault = false`؛ صدور `ShippingCreatedEvent` |
| `Update` | به‌روزرسانی مشخصات؛ صدور `ShippingUpdatedEvent` و در صورت تغییر هزینه، `ShippingCostChangedEvent` |
| `CalculateCost(orderTotal, shippingMultiplier)` | روش غیرفعال → صفر؛ رسیدن به آستانه ارسال رایگان → صفر؛ در غیر این صورت `BaseCost × multiplier` (ضریب نامعتبر یک در نظر گرفته می‌شود) با گرد کردن به عدد صحیح |
| `CalculateCostForCart(orderTotal, items)` | مجموع `multiplier × quantity` آیتم‌ها ضریب کل را می‌سازد؛ در نبود آیتم، هزینه پایه |
| `IsAvailableForOrder` / `ValidateForOrder` | غیرفعال بودن → شکست «روش ارسال غیرفعال است.»؛ سپس اعتبارسنجی `OrderRange` |
| `SetAsDefault` | روش غیرفعال نمی‌تواند پیش‌فرض شود؛ صدور `ShippingSetAsDefaultEvent` |
| `RequestDeletion` | روش پیش‌فرض قابل حذف نیست (`DefaultShippingCannotBeDeletedException`)؛ غیرفعال‌سازی + `ShippingDeletedEvent` |
| `Restore` | فعال‌سازی مجدد بدون رویداد |
| `QualifiesForFreeShipping` / `GetDeliveryTimeDisplay` | بررسی آستانه و نمایش «X تا Y روز کاری» |

### ValueObjectها

| ValueObject | قاعده |
|---|---|
| `ShippingName` | ۲ تا ۱۰۰ کاراکتر با پیام‌های فارسی |
| `DeliveryTimeRange` | حداقل ≥ ۰، حداکثر ≥ حداقل و حداکثر ≤ ۳۶۵ روز |
| `FreeShippingThreshold` | فعال‌بودن مستلزم مبلغ ≥ ۰؛ شرط ارسال رایگان `orderTotal >= threshold` |
| `ShippingOrderRange` | سه حالت نامحدود، دارای حداقل و دارای حداکثر با پیام‌های «حداقل/حداکثر مبلغ سفارش برای این روش ارسال ... است.» |
| `ShippingCostItem` / `ShippingAssignment` | اقلام محاسبه هزینه و اتصال ارسال به واریانت |

**منطقه (Zone) وجود ندارد**: به‌جای ناحیه‌بندی، بازه مبلغ سفارش (`ShippingOrderRange`) اعمال می‌شود.

Exception اختصاصی: `DefaultShippingCannotBeDeletedException` با کد `DEFAULT_SHIPPING_CANNOT_BE_DELETED`.

Domain Eventها (۵): `ShippingCreatedEvent`, `ShippingUpdatedEvent`, `ShippingCostChangedEvent`,
`ShippingSetAsDefaultEvent`, `ShippingDeletedEvent`. هیچ Handlerی به این رویدادها گوش نمی‌دهد.
پوشه‌های `Enums`، `Rules` و `Specifications` خالی‌اند. سرویس دامنه `ShippingDomainService.CalculateShippingCost`
خروجی `ShippingCostCalculationResult` (موفقیت، هزینه، ارسال رایگان، زمان تحویل، خطا) می‌سازد.

## Application (`Application/Shipping`)

**۵ Command:** `CreateShipping`, `UpdateShipping`, `DeleteShipping`, `RestoreShipping`, `SetDefaultShipping`
(Validator برای Create و Update).

**۶ Query:** `GetShippings` (`ICacheableQuery` با کلید `shippings:list:inactive={IncludeInactive}` و
انقضای ۳۰ دقیقه), `GetShipping`, `CalculateShippingCost`, `GetAvailableShippings`,
`GetAvailableShippingsForVariants`, `GetShippingQuotes`. همه از `IShippingQueryService` استفاده می‌کنند و
چهار Validator دارند.

نکات Handlerها: Create/Update کش را با پیشوند `shippings:` پاک می‌کنند؛ `DeleteShippingHandler` خطای
دامنه «روش پیش‌فرض قابل حذف نیست» را می‌گیرد و پاسخ شکست می‌دهد؛ `SetDefaultShippingHandler` پیش‌فرض
قبلی را `UnsetDefault` و روش جدید را `SetAsDefault` می‌کند؛ `RestoreShippingHandler` رویداد حسابرسی
`RestoreShippingMethod` ثبت می‌کند. این ماژول Event Handler ندارد.

## API

جمع: **۳ کنترلر و ۱۱ اکشن**. قراردادهای عمومی در [05-api/conventions.md](../05-api/conventions.md).

| کنترلر | مسیر (verbatim) | دسترسی | اکشن‌ها |
|---|---|---|---|
| `ShippingController` | `[Route("api/v{version:apiVersion}/shipping")]` | `[AllowAnonymous]` | `GET` → `GetActiveShippings` |
| `CheckoutShippingController` | `[Route("api/v{version:apiVersion}/checkout/shipping")]` | `[Authorize]` | `GET available` → `GetAvailableShippings`، `GET cost` → `CalculateShippingCost`، `POST available` → `GetAvailableShippingsForVariants`، `POST quotes` → `GetShippingQuotes` |
| `AdminShippingController` | `[Route("api/v{version:apiVersion}/admin/shipping")]` | `[Authorize(Roles = "Admin")]` | `GET` → `GetShippings`، `GET {id:guid}` → `GetShippingById`، `POST` → `CreateShipping`، `POST {id:guid}/restore` → `RestoreShipping`، `PUT {id:guid}` → `UpdateShipping`، `DELETE {id:guid}` → `DeleteShipping` |

## زیرساخت

- `ShippingConfiguration` ایندکس یگانه روی `Name` و ایندکس‌های `IsActive` و `IsDefault` دارد؛
  `ShippingRepository` و `ShippingQueryService` نوشتن و خواندن را تأمین می‌کنند.
- کش در سطح Application با `ICacheService`/`ICacheableQuery` انجام می‌شود و در Infrastructure نگهداری نمی‌شود
  (سازوکار کلی در [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)).

## نکات و محدودیت‌ها

- **محاسبه هزینه وزنی نیست**: مدل واقعی «هزینه پایه × ضریب واریانت» است. `MaxWeight` در Aggregate وجود
  دارد و در `VariantShipping` وزن هر واریانت ذخیره می‌شود، اما در هیچ محاسبه‌ای استفاده نمی‌شود.
- `ShippingAssignment` تعریف شده اما در منطق Aggregate به‌کار نمی‌رود.
- `DeleteShippingHandler` در خطای «روش پیش‌فرض قابل حذف نیست» Exception دامنه را می‌گیرد و پاسخ شکست
  برمی‌گرداند؛ رفتار حذف از مسیر `RequestDeletion` دامنه (غیرفعال‌سازی) می‌گذرد و حذف فیزیکی از این
  مسیر انجام نمی‌شود.
- پارامتر `includeDeleted` در اکشن ادمین به Query با نام `GetShippingsQuery` سپرده می‌شود اما فیلد
  متناظر در Query `IncludeInactive` است؛ همراستایی نام‌ها در کد یکدست نیست.
- ماژول Shipping رویدادهایش را برای هیچ مصرف‌کننده‌ای منتشر نمی‌کند؛ تغییر هزینه ارسال در سفارش‌های
  در جریان بازتاب داده نمی‌شود.

## اسناد مرتبط

- اعتبارسنجی ارسال و هزینه در ثبت سفارش: [orders.md](orders.md)
- ضریب ارسال هر واریانت: [variants.md](variants.md)
- تخفیف ارسال رایگان: [discounts.md](discounts.md)
- سبد خرید و محاسبه هزینه: [cart.md](cart.md)
