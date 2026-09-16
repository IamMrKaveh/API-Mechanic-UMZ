[بازگشت به فهرست ماژول‌ها](README.md)

# کدهای تخفیف (Discounts)

`DiscountCode` کد تخفیف درصدی، مبلغ ثابت یا ارسال رایگان را با سقف استفاده، بازه اعتبار و محدودیت‌های
اختیاری نگه می‌دارد و هر مصرف را در `DiscountUsageRecord` ثبت می‌کند. Application آن پنج Command و پنج
Query دارد و ۱۰ اکشن در دو کنترلر ارائه می‌کند. اعمال تخفیف در زمان ثبت سفارش در [orders.md](orders.md)
و هزینه ارسال در [shipping.md](shipping.md) مستند شده است.

## دامنه (`Domain/Discount`)

`DiscountCode : AggregateRoot<DiscountCodeId>, ISoftDeletable` با `Code`, `Value`, `MaximumDiscountAmount?`,
`UsageLimit?`, `UsageCount`, `StartsAt?`, `ExpiresAt?`, `IsActive` و مجموعه‌های `_restrictions` و `_usages`.
کد با `Trim().ToUpperInvariant()` نرمال می‌شود و در دیتابیس ایندکس یگانه دارد.

| متد | قاعده/اثر |
|---|---|
| `Create` | کد الزامی؛ `ExpiresAt` باید بعد از `StartsAt` باشد؛ صدور `DiscountCodeCreatedEvent` |
| `Update` | همان قاعده تاریخ‌ها؛ بدون رویداد |
| `ValidateForApplication(orderAmount, now)` | شکست در صورت غیرفعال بودن، شروع‌نشدن، انقضا، رسیدن به `UsageLimit` و نقض محدودیت `MinimumOrderAmount` |
| `CalculateDiscount(orderAmount)` | اعمال `DiscountValue`، سقف `MaximumDiscountAmount` و محدودسازی به مبلغ سفارش |
| `RecordUsage(userId, orderId, discountedAmount, now)` | در صورت غیرقابل استفاده بودن `DiscountCodeNotRedeemableException`؛ افزایش `UsageCount`، ساخت رکورد مصرف و صدور `DiscountCodeAppliedEvent` |
| `IsRedeemable(now)` | ترکیب فعال‌بودن، شروع‌شده، منقضی‌نشده و نرسیدن به سقف مصرف |
| `Activate` / `Deactivate` | idempotent با رویداد متناظر |

### انواع تخفیف

`DiscountValue` سه نوع دارد: `Percentage` (۰ تا ۱۰۰)، `FixedAmount` (مبلغ مثبت) و `FreeShipping`
(مقدار صفر). `DiscountType` enum متناظر است: `Percentage=1`, `FixedAmount=2`, `FreeShipping=3`.

`DiscountRestrictionType` شش مقدار دارد: `MinimumOrderAmount`, `SpecificProduct`, `SpecificCategory`,
`SpecificUser`, `FirstOrderOnly`, `MaximumUsagePerUser`. **فقط `MinimumOrderAmount` در
`ValidateForApplication` اجرا می‌شود** و بقیه مقادیر در کد اعمال نمی‌شوند.

`DiscountUsageRecord : Entity<DiscountUsageId>` با `Code`, `DiscountedAmount`, `UsageCountAtTime`,
`UsedAt` و کلیدهای `DiscountCodeId`/`UserId`/`OrderId`.

Exceptionها: `DiscountCodeNotRedeemableException` (`DISCOUNT_CODE_NOT_REDEEMABLE`) و
`InvalidDiscountException` (`INVALID_DISCOUNT`).

Domain Eventها (۴): `DiscountCodeCreatedEvent`, `DiscountCodeAppliedEvent`, `DiscountCodeActivatedEvent`,
`DiscountCodeDeactivatedEvent`. هیچ مصرف‌کننده‌ای برای این رویدادها وجود ندارد و پوشه `Rules/` خالی است.

## Application (`Application/Discount`)

**۵ Command:** `CreateDiscount` (Validator: کد حداکثر ۵۰ کاراکتر، مقدار مثبت به‌جز ارسال رایگان، درصد
حداکثر ۱۰۰، سقف‌ها مثبت و `ExpiresAt > StartsAt`)، `UpdateDiscount`, `DeleteDiscount`,
`ApplyDiscount`, `CancelDiscountUsage`.

**۵ Query:** `GetDiscounts`, `GetDiscountById`, `GetDiscountInfo`, `ValidateDiscount`, `GetDiscountUsageReport`.

نکات جریان‌ها:

- `CreateDiscountHandler` کد تکراری را با پیام «کد تخفیف تکراری است...» و پاسخ ۴۰۹ رد می‌کند.
- `DeleteDiscountHandler` فقط غیرفعال می‌کند و حذف فیزیکی/نرم انجام نمی‌دهد.
- `ApplyDiscountHandler` کد را می‌خواند، اعتبارسنجی می‌کند، `CalculateDiscount` را روی مبلغ سفارش
  اجرا می‌کند، `finalAmount = orderAmount − discountAmount` را با ارز `"IRT"` می‌سازد، `RecordUsage`
  را صدا می‌زند و رویداد حسابرسی `DiscountApplied` را ثبت می‌کند.
- `ValidateDiscountQuery` از `IDiscountQueryService.ValidateDiscountAsync` استفاده می‌کند و
  `IDiscountService.ApplyDiscountAsync` (Infrastructure) همان منطق را برای ماژول Order فراهم می‌کند.
- `CancelDiscountUsageCommand` مصرف متناظر سفارش را پیدا و رویداد «لغو استفاده» را ثبت می‌کند اما
  **`UsageCount` را کاهش نمی‌دهد و رکورد مصرف را حذف نمی‌کند**.
- این ماژول Event Handler ندارد.

## API

جمع: **۲ کنترلر و ۱۰ اکشن**. قراردادهای عمومی در [05-api/conventions.md](../05-api/conventions.md).

`DiscountsController` — `[Route("api/v{version:apiVersion}/discounts")]` — `[Authorize]`:

| متد | مسیر | اکشن |
|---|---|---|
| POST | `validation` | `Validate` |
| POST | `application` | `Apply` |

`AdminDiscountsController` — `[Route("api/v{version:apiVersion}/admin/discounts")]` — `[Authorize(Roles = "Admin")]`:

| متد | مسیر | اکشن | دسترسی اکشن |
|---|---|---|---|
| GET | — | `GetAll` | سطح کلاس |
| GET | `{id:guid}` | `GetById` | سطح کلاس |
| GET | `{id:guid}/usage-report` | `GetUsageReport` | سطح کلاس |
| GET | `codes/{code}` | `GetDiscountInfo` | **`[AllowAnonymous]`** (اطلاعات عمومی کد) |
| POST | — | `Create` | سطح کلاس |
| PUT | `{id:guid}` | `Update` | سطح کلاس |
| DELETE | `{id:guid}` | `Delete` | سطح کلاس |
| DELETE | `{id:guid}/usage` | `CancelDiscountUsage` | سطح کلاس |

اکشن عمومی `GetDiscountInfo` با اینکه داخل کنترلر مدیریتی است، `[AllowAnonymous]` دارد؛ یعنی کد تخفیف
بدون احراز هویت قابل استعلام است.

## زیرساخت

- نگاشت EF در `DiscountCodeConfiguration` (ایندکس یگانه روی `Code`)، `DiscountRestrictionConfiguration` و
  `DiscountUsageConfiguration` با ایندکس‌های `DiscountCodeId`, `UserId`, `OrderId` انجام می‌شود.
- سمت خواندن `DiscountQueryService` و سرویس اعمال `DiscountService` هر دو در
  `Infrastructure/Discount/` هستند.
- **این ماژول کش ندارد**؛ نه `ICacheableQuery` و نه ابطال کش در Commandها دیده می‌شود.

## نکات و محدودیت‌ها

- محدودیت‌های `SpecificProduct`, `SpecificCategory`, `SpecificUser`, `FirstOrderOnly` و
  `MaximumUsagePerUser` تعریف شده‌اند اما در کد اجرا نمی‌شوند؛ بنابراین «سقف مصرف به ازای کاربر» عملاً
  وجود ندارد و فقط سقف کلی `UsageLimit` اعمال می‌گردد.
- `CancelDiscountUsageCommand` شمارنده مصرف را برنمی‌گرداند؛ در لغو سفارش، سقف مصرف تخفیف مصرف‌شده باقی
  می‌ماند.
- `DeleteDiscount` غیرفعال‌سازی است و ستون‌های حذف نرم را تغییر نمی‌دهد.
- در working tree فعلی `DiscountCode` و `DiscountUsageRecord` پارامتر `DateTime now` گرفته‌اند اما
  Handlerها هنوز امضای قبلی را صدا می‌زنند؛ ناسازگاری موقتی دو لایه است.
- نوع تخفیف در درخواست‌های API به‌صورت رشته دریافت می‌شود و در نگاشت، مقدار نامعتبر به‌طور پیش‌فرض
  `Percentage` تفسیر می‌گردد.

## اسناد مرتبط

- اعمال تخفیف در ثبت سفارش: [orders.md](orders.md) — سبد خرید: [cart.md](cart.md)
- ارسال رایگان و هزینه ارسال: [shipping.md](shipping.md)
- نگاشت و پلی‌پ‌لاین: [02-architecture/cqrs-features.md](../02-architecture/cqrs-features.md)
