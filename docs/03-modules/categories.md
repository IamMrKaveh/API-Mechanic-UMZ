[بازگشت به فهرست ماژول‌ها](README.md)

# دسته‌بندی (Categories)

`Category` گروه‌بندی محصولات است: نام یکتا، اسلاگ، توضیحات، ترتیب نمایش و وضعیت فعال.
Application آن چهار Command و هفت Query دارد و در دو کنترلر عمومی و مدیریتی ده اکشن ارائه می‌کند.
برندها به دسته متصل می‌شوند؛ جزئیات در [brands.md](brands.md).

## دامنه (`Domain/Category`)

`Category : AggregateRoot<CategoryId>` با `Name`, `Slug`, `Description`, `IsActive`, `SortOrder`,
`CreatedAt`, `UpdatedAt` و فهرست `Brands` (شناسه‌های برند).

| متد | قاعده/اثر |
|---|---|
| `Create` | یکتایی نام/اسلاگ از طریق `ICategoryUniquenessChecker`؛ در تکرار `DuplicateCategoryNameException`؛ صدور `CategoryCreatedEvent` |
| `UpdateDetails` | همان قاعده یکتایی؛ صدور `CategoryUpdatedEvent` |
| `Activate` / `Deactivate` | **idempotent**؛ اگر وضعیت همان باشد بی‌اثر است |

ValueObjectها: `CategoryId`, `CategoryName` (۲ تا ۱۰۰ کاراکتر؛ برابری بدون حساسیت به بزرگی/کوچکی)،
`CategorySlug` بر پایه `Slug` با سقف ۲۰۰ کاراکتر و تولید خودکار از نام.

Exception: `DuplicateCategoryNameException` با کد `DUPLICATE_CATEGORY_NAME`.
Domain Eventها (۴): `CategoryCreatedEvent`, `CategoryUpdatedEvent`, `CategoryActivatedEvent`,
`CategoryDeactivatedEvent`. پوشه `Services/` خالی و پوشه `Rules/` وجود ندارد.

**سلسله‌مراتب دسته‌بندی در دامنه وجود ندارد**: هیچ `ParentId`، مجموعه فرزندان، سقف عمق یا جلوگیری از
حلقه در Aggregate نیست. `CategoryTreeDto` یک فیلد `Children` دارد اما `GetCategoryTreeAsync` در
`Infrastructure/Category/QueryServices/CategoryQueryService.cs` فهرستی تخت برمی‌گرداند و این فیلد را
هرگز پر نمی‌کند.

## Application (`Application/Category`)

**۴ Command:** `CreateCategory` (Auditable؛ اسلاگ خودکار و ابطال کش با پیشوند `categories:`)،
`UpdateCategory` (همزمانی خوش‌بینانه با `RowVersion`، فعال/غیرفعال‌سازی از روی فلگ)،
`DeleteCategory`، `ReorderCategories`. سه Validator وجود دارد (`CreateCategory`, `UpdateCategory`, `ReorderCategories`).

**۷ Query:** `GetCategories`, `GetPublicCategories`, `GetCategory`, `GetCategoryProducts`,
`GetCategoryTree` (`ICacheableQuery` با کلید `categories:tree` و انقضای یک ساعت)، `GetCategoryWithBrands`,
`GetAdminCategories`. همه به `ICategoryQueryService` واگذار می‌کنند.

قواعد مهم:

- `DeleteCategoryHandler` حذف نرم انجام می‌دهد: اگر `HasBrandAsync` درست باشد، حذف با پیام
  «دسته‌بندی دارای زیرمجموعه است...» رد می‌شود؛ در غیر این صورت فقط `Deactivate()` صدا زده می‌شود.
- ماژول Category هیچ Event Handler ندارد؛ هیچ بخشی از کد به رویدادهای آن واکنش نشان نمی‌دهد.

## API

جمع: **۲ کنترلر و ۱۰ اکشن**. قراردادهای عمومی در [05-api/conventions.md](../05-api/conventions.md).

`CategoryController` — `[Route("api/v{version:apiVersion}/categories")]` — بدون اتریبیوت در سطح کلاس؛ اکشن‌ها `[AllowAnonymous]`:

| متد | مسیر | اکشن |
|---|---|---|
| GET | — | `GetCategories` |
| GET | `tree` | `GetCategoryHierarchy` |
| GET | `{id:guid}` | `GetCategoryById` |
| GET | `{id:guid}/products` | `GetCategoryProducts` |

`AdminCategoryController` — `[Route("api/v{version:apiVersion}/admin/categories")]` — `[Authorize(Roles = "Admin")]`:

| متد | مسیر | اکشن |
|---|---|---|
| GET | — | `GetCategories` |
| GET | `{id:guid}` | `GetCategory` |
| POST | — | `CreateCategory` |
| PUT | `{id:guid}` | `UpdateCategory` |
| DELETE | `{id:guid}` | `DeleteCategory` |
| PATCH | `order` | `ReorderCategories` |

## زیرساخت

- `CategoryConfiguration` جدول `Categories` را با `RowVersion` از نوع `bytea` و `IsConcurrencyToken`
  می‌سازد (توکن همزمانی این ماژول برخلاف `Product`/`Brand` که `xmin` دارند)، ایندکس یگانه روی
  `Name` و `Slug`، سقف ۱۰۰۰ کاراکتر برای توضیحات و ایندکس ترکیبی `(IsActive, SortOrder)` دارد؛
  `Category.Brands` در نگاشت نادیده گرفته می‌شود.
- `CategoryRepository` علاوه بر CRUD، `ExistsByNameAsync`، `ExistsBySlugAsync` و `HasBrandAsync` را
  برای قواعد یکتایی و نگهبان حذف فراهم می‌کند و مقدار اصلی `RowVersion` را با
  `SetOriginalRowVersion` تنظیم می‌کند.
- فیلترهای `IsActive`/`IsDeleted` در `CategoryQueryService` دستی اعمال می‌شوند (فیلتر سراسری EF وجود ندارد).

## نکات و محدودیت‌ها

- **`ReorderCategories` بدنه درخواست را نادیده می‌گیرد**: اکشن با مقادیر جانشین
  `new(Guid.NewGuid(), 0)` و `new(Guid.NewGuid(), 1)` ساخته می‌شود، بنابراین ترتیب واقعی دسته‌ها را
  تغییر نمی‌دهد. این نقص در `Presentation/Category/Endpoints/AdminCategoryController.cs` است.
- درخت دسته‌بندی واقعی نیست: هم دامنه بدون والد/فرزند است و هم Query درخت را تخت برمی‌گرداند؛
  `GetCategoryProducts` نیز محصولات را بر اساس همان دسته تخت فیلتر می‌کند.
- `DeleteCategory` به `Deactivate` می‌رسد و ستون `IsDeleted` را تغییر نمی‌دهد؛ حذف واقعی در این ماژول
  انجام نمی‌شود.
- در working tree فعلی، متدهای `Category` پارامتر `DateTime now` گرفته‌اند ولی Handlerها هنوز امضای
  قبلی را صدا می‌زنند؛ این ناسازگاری موقتی بین دو لایه است و در نسخه کامیت‌شده وجود ندارد.

## اسناد مرتبط

- برندها و اتصال آن‌ها به دسته: [brands.md](brands.md)
- محصولات هر دسته: [products.md](products.md)
- همزمانی خوش‌بینانه و Interceptorها: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
