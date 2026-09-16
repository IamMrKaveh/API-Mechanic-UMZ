[بازگشت به فهرست ماژول‌ها](README.md)

# علاقه‌مندی‌ها (Wishlist)

ماژول Wishlist ساده‌ترین ماژول دامنه است: هر ردیف یک جفت «کاربر + محصول» است و فهرست علاقه‌مندی‌های هر
کاربر از تجمیع همان ردیف‌ها ساخته می‌شود. Application آن چهار Command و دو Query دارد و ۶ اکشن در یک
کنترلر کاربری و یک اکشن مدیریتی ارائه می‌کند. محصولات در [products.md](products.md) مستند شده‌اند.

## دامنه (`Domain/Wishlist`)

`Wishlist : AggregateRoot<WishlistId>, IAuditable` — **مدل «یک ردیف به ازای هر (UserId, ProductId)» است،
نه یک Aggregate ظرفی با مجموعه آیتم‌ها.** تنها متد آن `Create(userId, productId, now)` است که
`WishlistItemAddedEvent` صادر می‌کند. نه سقف تعداد آیتم و نه قاعده یکتایی در دامنه وجود ندارد؛
جلوگیری از تکرار در Handler و با `ExistsAsync` انجام می‌شود.

- ValueObject: `WishlistId` (Guid).
- Domain Event (۱): `WishlistItemAddedEvent(wishlistId, userId, productId)`.
- پوشه‌های `Enums`, `Entities`, `Exceptions`, `Services` در این ماژول وجود ندارند.
- `IWishlistRepository`: `AddAsync`, `RemoveAsync(userId, productId)`, `ClearAsync(userId)`,
  `GetByUserAndProductAsync`, `ExistsAsync`.

## Application (`Application/Wishlist`)

**۴ Command:** `AddToWishlist`, `RemoveFromWishlist`, `ToggleWishlist`, `ClearWishlist`.

**۲ Query:** `GetWishlistById` (صفحه‌بندی‌شده و دارای `TargetUserId` برای استفاده مدیریتی)،
`CheckWishlistStatus`.

نکات جریان‌ها:

- `AddToWishlistHandler` وجود و فعال‌بودن محصول را می‌سنجد (در غیر این صورت NotFound) و جفت تکراری را
  با پاسخ ۴۰۹ رد می‌کند.
- `ToggleWishlistHandler` با `IsInWishlistAsync` وضعیت را می‌خواند و بر اساس آن آیتم را اضافه یا حذف
  می‌کند و در خروجی مشخص می‌کند افزوده شد یا نه.
- این ماژول Event Handler ندارد و هیچ کشی هم استفاده نمی‌کند.

## API

جمع: **۲ کنترلر و ۶ اکشن**. قراردادهای عمومی در [05-api/conventions.md](../05-api/conventions.md).

`WishlistController` — `[Route("api/v{version:apiVersion}/wishlist")]` — `[Authorize]`؛ ۵ اکشن:

| متد | مسیر | اکشن |
|---|---|---|
| GET | — | `GetMyWishlist` |
| GET | `{productId:guid}` | `IsInWishlist` |
| POST | — | `ToggleWishlist` |
| DELETE | `{productId:guid}` | `RemoveFromWishlist` |
| DELETE | — | `ClearWishlist` |

`AdminWishlistController` — `[Route("api/v{version:apiVersion}/admin/wishlist")]` — `[Authorize(Roles = "Admin")]`؛ ۱ اکشن:
`GET {userId:guid}/wishlist` → `GetUserWishlist`.

هیچ‌کدام اتریبیوت Rate Limit اختصاصی ندارند.

## زیرساخت

- نگاشت در `WishlistConfiguration` و تبدیل شناسه در `WishlistIdConverter`؛ `WishlistRepository`
  نوشتن/حذف و `WishlistQueryService` خواندن را انجام می‌دهد.
- **بدون کش و بدون Job اختصاصی**: نه سبد خرید و نه علاقه‌مندی‌ها پاک‌سازی زمان‌بندی‌شده ندارند.

## نکات و محدودیت‌ها

- «یک لیست علاقه‌مندی به ازای هر کاربر» به‌صورت Aggregate وجود ندارد؛ حذف همه آیتم‌ها با
  `ClearAsync(userId)` روی ردیف‌ها انجام می‌شود.
- هیچ سقف تعداد آیتم یا محدودیت نرخ در این ماژول اعمال نمی‌شود.
- `WishlistItemAddedEvent` هیچ مصرف‌کننده‌ای ندارد (نه اعلان و نه همگام‌سازی).
- در working tree فعلی `Wishlist.Create` پارامتر `DateTime now` گرفته اما Handlerهای
  `AddToWishlist` و `ToggleWishlist` هنوز امضای دو‌آرگومانی را صدا می‌زنند؛ در نسخه کامیت‌شده این
  ناسازگاری وجود ندارد.

## اسناد مرتبط

- محصول و دسترس‌پذیری: [products.md](products.md)
- داشبورد کاربر (شمارش علاقه‌مندی‌ها): [identity/users.md](identity/users.md)
- اعلان‌ها: [notifications.md](notifications.md)
