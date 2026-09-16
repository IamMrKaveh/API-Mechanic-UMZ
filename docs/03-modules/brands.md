[بازگشت به فهرست ماژول‌ها](README.md)

# برندها (Brands)

`Brand` برند محصولات است و به یک دسته تعلق دارد: نام، اسلاگ، توضیحات، لوگو و وضعیت فعال.
Application آن چهار Command و پنج Query دارد و در دو کنترلر عمومی و مدیریتی هشت اکشن ارائه می‌کند.
دسته‌بندی در [categories.md](categories.md) و ذخیره‌سازی فایل لوگو در [media.md](media.md) مستند شده است.

## دامنه (`Domain/Brand`)

`Brand : AggregateRoot<BrandId>, ISoftDeletable` با `Name`, `Slug`, `Description`, `LogoPath?`,
`IsActive`, `CategoryId` و فهرست `Products`.

| متد | قاعده/اثر |
|---|---|
| `Create` | یکتایی نام «در محدوده دسته» و یکتایی اسلاگ از طریق `IBrandUniquenessChecker`؛ در تکرار `BrandNameAlreadyExistsException` |
| `UpdateDetails` | همان قاعده یکتایی |
| `ChangeCategory` | جابه‌جایی بین دسته‌ها؛ در تساوی بی‌اثر؛ صدور `BrandCategoryChangedEvent` |
| `Activate` / `Deactivate` | **idempotent نیستند**: فعال‌سازی برند فعال یا غیرفعال‌سازی برند غیرفعال Exception می‌دهد |
| `RequestDeletion` | حذف نرم (غیرفعال + `IsDeleted`)؛ صدور `BrandDeletedEvent` |

ValueObjectها: `BrandId`, `BrandName` (۲ تا ۱۰۰ کاراکتر)، `BrandSlug` بر پایه `Slug`.

Exceptionها (هر سه در فایل `BrandDomainException.cs`): `BrandNameAlreadyExistsException`
(`BRAND_NAME_ALREADY_EXISTS`)، `BrandAlreadyActiveException` (`BRAND_ALREADY_ACTIVE`)،
`BrandAlreadyDeactivatedException` (`BRAND_ALREADY_DEACTIVATED`).

Domain Eventها (۶): `BrandCreatedEvent`, `BrandUpdatedEvent`, `BrandCategoryChangedEvent`,
`BrandActivatedEvent`, `BrandDeactivatedEvent`, `BrandDeletedEvent`.

**اعتبارسنجی لوگو در دامنه نیست**: `LogoPath` یک رشته ساده است و قواعد حجم/نوع در لایه Application اعمال می‌شوند.

## Application (`Application/Brand`)

**۴ Command:** `CreateBrand`, `UpdateBrand`, `DeleteBrand`, `MoveBrand` — هر چهار Validator دارند.

**۵ Query:** `GetBrands`, `GetAdminBrands`, `GetBrand`, `GetBrandDetail`, `GetPublicBrands`
(`ICacheableQuery` با کلید `brands:public:category={...|all}` و انقضای ۳۰ دقیقه).

قواعد مهم:

- `CreateBrandHandler` و `UpdateBrandHandler` سقف فایل لوگو را `2 * 1024 * 1024` بایت (۲ مگابایت) و
  انواع مجاز را `image/jpeg`, `image/png`, `image/webp` تعیین می‌کنند؛ آپلود از طریق
  `IStorageService.UploadAsync(..., "brands", ct)` انجام می‌شود و کش `brands:` باطل می‌گردد.
  (جزئیات ذخیره‌سازی و پویش ویروس در [media.md](media.md).)
- `DeleteBrandHandler` به‌جای `RequestDeletion` متد `Deactivate()` را صدا می‌زند؛ یعنی حذف از این مسیر
  فقط غیرفعال‌سازی است.
- `MoveBrandHandler` وجود برند و دسته مقصد را اعتبارسنجی می‌کند.
- ماژول Brand هیچ Event Handler ندارد.

## API

جمع: **۲ کنترلر و ۸ اکشن**. قراردادهای عمومی در [05-api/conventions.md](../05-api/conventions.md).

`BrandController` — `[Route("api/v{version:apiVersion}/brands")]` — اکشن‌ها `[AllowAnonymous]`:

| متد | مسیر | اکشن |
|---|---|---|
| GET | — | `GetBrands` |
| GET | `{id:guid}` | `GetBrand` |

`AdminBrandController` — `[Route("api/v{version:apiVersion}/admin/brands")]` — `[Authorize(Roles = "Admin")]`:

| متد | مسیر | اکشن | اشاره |
|---|---|---|---|
| GET | — | `GetBrands` | `[FromQuery] GetAdminBrandsRequest` |
| GET | `{id:guid}` | `GetBrand` | — |
| POST | — | `CreateBrand` | `[FromForm]` (فایل لوگو) |
| PUT | `{id:guid}` | `UpdateBrand` | `[FromForm]` |
| DELETE | `{id:guid}` | `DeleteBrand` | — |
| PATCH | `move` | `MoveBrand` | `[FromBody]` |

## زیرساخت

- `BrandConfiguration` جدول `Brands` را با ستون سیستمی PostgreSQL (`xmin` از نوع `xid`) به‌عنوان
  توکن همزمانی می‌سازد، ایندکس یگانه روی `Slug` و ایندکس روی `CategoryId` دارد؛
  نام برند در دیتابیس ایندکس یگانه **ندارد** و یکتایی «نام در هر دسته» فقط در Repository بررسی می‌شود.
- `LogoPath` سقف ۱۰۰۰ کاراکتر دارد و `IsDeleted` به‌صورت پیش‌فرض نادرست است.
- `BrandQueryService` فیلترهای `IsActive`/`IsDeleted` را دستی اعمال می‌کند (بدون Query Filter سراسری).

## نکات و محدودیت‌ها

- تفاوت رفتار با دسته‌بندی: `Brand.Activate`/`Deactivate` در صورت تکرار وضعیت Exception می‌دهند،
  در حالی که `Category.Activate`/`Deactivate` idempotent‌اند.
- `Brand.RequestDeletion` در دامنه وجود دارد اما مسیر Endpoint مدیریتی آن را صدا نمی‌زند؛
  حذف از API به غیرفعال‌سازی می‌رسد.
- لوگو در Domain اعتبارسنجی نمی‌شود؛ محدودیت ۲ مگابایت و انواع تصویر فقط در Handlerها هستند،
  در حالی که ماژول Media سقف ۱۰ مگابایت و انواع بیشتری را برای محصول/دسته/برند می‌پذیرد.
- در working tree فعلی متدهای `Brand` پارامتر `DateTime now` گرفته‌اند اما Handlerها هنوز امضای
  قبلی را صدا می‌زنند؛ ناسازگاری موقتی دو لایه در نسخه کامیت‌شده وجود ندارد.

## اسناد مرتبط

- دسته‌بندی و نگهبان حذف دسته: [categories.md](categories.md)
- ذخیره‌سازی فایل، S3 و پویش ویروس: [media.md](media.md)
- محصولات و اتصال برند: [products.md](products.md)
