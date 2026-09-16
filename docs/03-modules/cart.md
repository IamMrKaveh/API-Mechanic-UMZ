[بازگشت به فهرست ماژول‌ها](README.md)

# سبد خرید (Cart)

سبد خرید برای کاربران عضو و مهمان‌ها (با `GuestToken`) کار می‌کند، قیمت را در لحظه افزودن اسنپ‌شات
می‌گیرد و در زمان ثبت سفارش به ماژول Order تحویل می‌شود. Application آن شش Command و سه Query دارد و
همه در یک کنترلر با ۹ اکشن ارائه می‌شوند. جریان ثبت سفارش در [orders.md](orders.md) و قیمت‌ها در
[variants.md](variants.md) مستند شده‌اند.

## دامنه (`Domain/Cart`)

`Cart : AggregateRoot<CartId>` با `GuestToken?`, `IsCheckedOut`, `UserId?`, `AppliedDiscountCodeId?`،
مجموعه `CartItems` و ویژگی‌های محاسبه‌شده `IsEmpty` و `TotalAmount`.

| متد | قاعده/اثر |
|---|---|
| `CreateForUser` / `CreateForGuest` | ساخت سبد با صدور `CartCreatedEvent` |
| `AddItem` | منع سبد تسویه‌شده (`EnsureNotCheckedOut`)؛ اگر واریانت موجود باشد `IncrementQuantity` وگرنه آیتم جدید؛ صدور `CartItemAddedEvent` و افزایش نسخه برای همزمانی |
| `RemoveItem` | نبود آیتم → `CartItemNotFoundException`؛ صدور `CartItemRemovedEvent` |
| `UpdateItemQuantity` | تغییر تعداد بدون رویداد |
| `RefreshItemPrice` | همگام‌سازی قیمت واحد/اصلی با مقدار جدید |
| `Clear` | خالی‌کردن بدون رویداد |
| `Checkout` | سبد خالی ممنوع؛ `IsCheckedOut = true`؛ صدور `CartCheckedOutEvent` |
| `AssignToUser` | تبدیل سبد مهمان به سبد کاربر و پاک‌کردن `GuestToken` |
| `MergeFrom` | ادغام دو سبد طبق استراتژی؛ الزام «سبد مقصد متعلق به کاربر»؛ صدور `CartMergedEvent` |

`CartItem : Entity<CartItemId>` با `ProductName`, `VariantSku`, `SellingPrice`, `OriginalPrice`,
`Quantity`, `AddedAt`, `VariantId`, `ProductId`. قیمت‌ها در لحظه افزودن کپی می‌شوند (`.Copy()`) و
`OriginalPrice` برای نمایش قیمت خط‌خورده نگه داشته می‌شود. تعداد باید بزرگ‌تر از صفر باشد وگرنه
`InvalidCartQuantityException`.

ValueObjectها: `CartId`, `CartItemId`, `GuestToken` (حداقل ۸ کاراکتر؛ `Generate()` خروجی
Guid با حروف بزرگ).
Enum `CartMergeStrategy`: `KeepHigherQuantity`, `SumQuantities` (پیش‌فرض), `KeepUserCart`, `KeepGuestCart`.

Exceptionها (همه در `CartDomainException.cs`): `CartItemNotFoundException` (`CART_ITEM_NOT_FOUND`)،
`CartAlreadyCheckedOutException` (`CART_ALREADY_CHECKED_OUT`)،
`InvalidCartQuantityException` (`INVALID_CART_QUANTITY`).

Domain Eventها (۵): `CartCreatedEvent`, `CartItemAddedEvent`, `CartItemRemovedEvent`,
`CartMergedEvent`, `CartCheckedOutEvent`. **هیچ Handlerی در Application یا Infrastructure به این
رویدادها گوش نمی‌دهد.** پوشه `Services/` خالی است.

**قاعده سقف تعداد آیتم در دامنه وجود ندارد**؛ سقف‌ها فقط در Validatorهای Application هستند.

## Application (`Application/Cart`)

**۶ Command:** `AddItemToCart` (Validator: تعداد ۱ تا ۱۰۰)، `UpdateCartItemQuantity`
(Validator: ۰ تا ۱۰۰۰؛ مقدار صفر از Validator می‌گذرد ولی قاعده دامنه (> ۰) آن را رد می‌کند)،
`RemoveItemFromCart` (با `IManualTransactionRequest`)، `ClearCart`, `MergeGuestCart`
(پارامتر `Strategy` با پیش‌فرض `SumQuantities`), `SyncCartPrices`.

**۳ Query:** `GetCart`, `GetCartSummary`, `ValidateCartForCheckout`. هیچ Event Handler و هیچ Validator
برای Queryها وجود ندارد.

نکات جریان‌ها:

- تشخیص هویت در همه Handlerها یکسان است: `ICurrentUserService.UserId` یا هدر مهمان `X-Guest-Token`؛
  در نبود هر دو خطا برگردانده می‌شود. سبد کاربر با فیلتر `!IsCheckedOut` جست‌وجو می‌شود.
- `AddItemToCartHandler` فعال‌بودن واریانت و `inventory.CanFulfill(quantity)` را می‌سنجد و سبد را
  به‌صورت تنبل (lazy) می‌سازد.
- `MergeGuestCartHandler` اگر سبد کاربر نباشد آن را `AssignToUser` می‌کند و در غیر این صورت `MergeFrom`
  را اجرا و سبد مهمان را حذف می‌کند و لاگ حسابرسی می‌نویسد. **ادغام خودکار در زمان ورود وجود ندارد**؛
  فقط Endpoint `POST cart/merge` آن را فعال می‌کند.
- `SyncCartPricesHandler` قیمت جاری هر واریانت را می‌خواند و `RefreshItemPrice` را صدا می‌زند.
- `ValidateCartForCheckout` در `Infrastructure/Cart/QueryServices/CartQueryService.cs` فقط وجود و
  خالی‌نبودن سبد را بررسی می‌کند؛ بررسی موجودی و قیمت در این Query انجام نمی‌شود و DTO آن فیلدهای
  مربوطه را دارد.

## API

جمع: **۱ کنترلر و ۹ اکشن**. قراردادهای عمومی در [05-api/conventions.md](../05-api/conventions.md).

`CartsController` (نام کنترلر `CartController`) — `[Route("api/v{version:apiVersion}/cart")]` — `[Authorize]` در سطح کلاس:

| متد | مسیر | اکشن | دسترسی اکشن |
|---|---|---|---|
| GET | — | `GetCart` | `[AllowAnonymous]` |
| GET | `summary` | `GetCartSummary` | `[AllowAnonymous]` |
| GET | `checkout/validation` | `ValidateCartForCheckout` | سطح کلاس (`[Authorize]`) |
| POST | `items` | `AddItem` | `[AllowAnonymous]` |
| PUT | `items/{variantId:guid}` | `UpdateQuantity` | `[AllowAnonymous]` |
| DELETE | `items/{variantId:guid}` | `RemoveItem` | `[AllowAnonymous]` |
| DELETE | — | `ClearCart` | `[AllowAnonymous]` |
| POST | `merge` | `MergeCart` | سطح کلاس (`[Authorize]`) |
| PUT | `prices` | `SyncCartPrices` | سطح کلاس (`[Authorize]`) |

اکشن‌های `[AllowAnonymous]` برای پشتیبانی از سبد مهمان باز شده‌اند؛ تشخیص هویت در آن‌ها از هدر
`X-Guest-Token` انجام می‌شود.

## زیرساخت

- **ذخیره‌سازی روی EF Core و PostgreSQL است، نه Redis**: `CartRepository` از `DBContext` استفاده
  می‌کند و در لایه‌های Cart هیچ ارجاعی به Redis وجود ندارد. جدول‌ها `Carts` و `CartItems` با
  `Money` به‌صورت نوع مالک‌شده، Interceptor نسخه‌گذاری سطر و ایندکس‌های `(UserId, IsCheckedOut)` و
  `(GuestToken, IsCheckedOut)` هستند.
- `ICartRepository` فقط سبدهای تسویه‌نشده را برمی‌گرداند (`FindByIdAsync`, `FindByUserIdAsync`,
  `FindByGuestTokenAsync` همه با فیلتر `!IsCheckedOut`)؛ در حذف کاربر مقدار `SetNull` اعمال می‌شود.
- **هیچ Job پاک‌سازی یا انقضایی برای سبدها وجود ندارد**؛ سبدهای قدیمی حذف نمی‌شوند و تنها با فیلتر
  `!IsCheckedOut` از نتایج کنار می‌روند.

## نکات و محدودیت‌ها

- سبد تسویه‌شده هرگز پاک یا بازنشانی نمی‌شود؛ `Checkout()` فقط پرچم `IsCheckedOut` را روشن می‌کند و
  برای خرید بعدی سبد تازه ساخته می‌شود. سیاست نگهداشت سبد در کد مشخص نیست.
- در working tree فعلی متدهای `Cart`/`CartItem` پارامتر `DateTime now` گرفته‌اند اما Handlerها هنوز
  امضای قبلی را صدا می‌زنند؛ در نسخه کامیت‌شده این ناسازگاری وجود ندارد.
- رویدادهای سبد مصرف‌کننده ندارند؛ نه اعلان، نه همگام‌سازی جست‌وجو و نه ابطال کشی به آن‌ها گره نخورده است.
- `ValidateCartForCheckout` به‌رغم نامش موجودی/قیمت را بررسی نمی‌کند؛ این کار در
  `CheckoutStockValidatorService` و `CheckoutPriceValidatorService` داخل ماژول Order انجام می‌شود.

## اسناد مرتبط

- ثبت سفارش از سبد (`CheckoutFromCart`): [orders.md](orders.md)
- قیمت و واریانت: [variants.md](variants.md) — موجودی: [inventory.md](inventory.md)
- سبد مهمان و هویت: [identity/auth-security.md](identity/auth-security.md)
- کش و قفل توزیع‌شده: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
