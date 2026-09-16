[بازگشت به فهرست ماژول‌ها](README.md)

# فایل‌ها و رسانه‌ها (Media)

ماژول Media نگهداشت رسانه‌های متصل به موجودیت‌ها (محصول، برند، دسته) را بر عهده دارد: متادیتای فایل در
دیتابیس و خود فایل در ذخیره‌سازی شیئی سازگار با S3. Application آن پنج Command و سه Query دارد و ۸ اکشن
در دو کنترلر عمومی و مدیریتی ارائه می‌کند. لوگوی برند در [brands.md](brands.md) و پاک‌سازی فایل‌های
بی‌صاحب در [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md) آمده است.

## دامنه (`Domain/Media`)

`Media : AggregateRoot<MediaId>, IAuditable, IActivatable, ISoftDeletable` با `Path` (از نوع `FilePath`),
`Size` (از نوع `FileSize`), `FileType`, `EntityType`, `EntityId`, `SortOrder`, `IsPrimary`, `AltText?`,
`IsActive` و نشانه‌های حذف نرم. ویژگی‌های محاسبه‌شده: `FilePath`, `FileName`, `Extension`, `FileSize`.

| متد | قاعده/اثر |
|---|---|
| `Create` | نوع فایل الزامی؛ `SortOrder` منفی ممنوع («ترتیب نمایش نمی‌تواند منفی باشد»)؛ `AltText` حداکثر ۵۰۰ کاراکتر؛ نوع فایل به حروف کوچک نرمال می‌شود |
| `SetAsPrimary` | الزام فعال و حذف‌نشده بودن (`EnsureActive`)؛ صدور `MediaSetAsPrimaryEvent` |
| `RemovePrimary` | idempotent |
| `RequestDeletion` | حذف نرم + پاک‌کردن `IsPrimary`/`IsActive`؛ صدور `MediaDeletedEvent` |
| `CanBeSetAsPrimary` | فعال، غیرپیش‌فرض و حذف‌نشده |

**سقف حجم در Aggregate نیست**؛ محدودیت‌ها به `FileSize` در `SharedKernel` (سقف ۱۰۰ مگابایت) و لایه
Application سپرده شده‌اند.

`MediaDomainService` (`Domain/Media/Services/`): ثابت‌های `MediaEntityTypes` (`"Product"`, `"Brand"`,
`"Category"`)، `MaxMediaPerEntity = 20` (**اعلام شده اما در کد اعمال نمی‌شود**)،
`ValidateFileTypeForEntity` (برای محصول/برند/دسته فقط تصویر و برای بقیه تصویر/سند/ویدئو) و
`SelectNewPrimaryAfterDeletion` (انتخاب اولین رسانه فعال بر اساس `SortOrder` و سپس `CreatedAt`).

ValueObjectهای `SharedKernel` مورد استفاده: `FilePath` (سقف ۵۰۰ کاراکتر؛ رد کاراکترهای
`.. : * ? " < > |`؛ کمکی‌های `IsImage`/`IsDocument`/`IsVideo` و `GetContentType`), `FileSize`
(سقف ۱۰۰ مگابایت) و `Slug` (سقف ۲۰۰ کاراکتر با پشتیبانی حروف فارسی).

Exception: `InvalidFileTypeException` با کد `INVALID_FILE_TYPE`.
Domain Eventها (۳): `MediaCreatedEvent`, `MediaDeletedEvent`, `MediaSetAsPrimaryEvent`.

## Application (`Application/Media`)

**۵ Command:** `UploadMedia`, `SetPrimaryMedia`, `ReorderMedia`, `DeleteMedia`, `CleanupOrphanedMedia`.

**۳ Query:** `GetAllMedia`, `GetEntityMedia`, `GetMediaById`.

نکات جریان‌ها:

- `UploadMediaHandler` مسیر را با `FilePath.CreateForUpload` و حجم را با `FileSize.Create` می‌سازد،
  نوع فایل را با `MediaDomainService.ValidateFileTypeForEntity` می‌سنجد و کار را به `IMediaService.UploadAsync`
  می‌سپارد. Validator: انواع مجاز `image/jpeg`, `image/png`, `image/webp`, `image/gif` و سقف ۱۰ مگابایت؛
  `EntityType`، `EntityId` و `FileName` الزامی.
- `CleanupOrphanedMediaHandler` برای هر مسیر ذخیره‌شده در دیتابیس بررسی می‌کند فایل در ذخیره‌سازی وجود
  دارد یا نه؛ فایل‌های غایب حذف نرم می‌شوند و رویداد حسابرسی `OrphanedMediaCleanup` ثبت می‌گردد.
- این ماژول Event Handler ندارد.

## API

جمع: **۲ کنترلر و ۸ اکشن**. قراردادهای عمومی در [05-api/conventions.md](../05-api/conventions.md).

`MediaController` — `[Route("api/v{version:apiVersion}/media")]` — اکشن‌ها `[AllowAnonymous]`:

| متد | مسیر | اکشن |
|---|---|---|
| GET | `{entityType}/{entityId}` | `GetMediaForEntity` |
| GET | `{id:guid}` | `GetMediaById` |

`AdminMediaController` — `[Route("api/v{version:apiVersion}/admin/media")]` — `[Authorize(Roles = "Admin")]`:

| متد | مسیر | اکشن | اشاره |
|---|---|---|---|
| GET | — | `GetAllMedia` | صفحه‌بندی و فیلتر نوع موجودیت |
| POST | — | `UploadMedia` | `[RequestSizeLimit(10_485_760)]`, `[Consumes("multipart/form-data")]`, `[FromForm]` |
| DELETE | `orphaned` | `CleanupOrphaned` | پاک‌سازی رسانه‌های بی‌صاحب |
| DELETE | `{mediaId:guid}` | `DeleteMedia` | حذف نرم |
| PATCH | `primary` | `SetPrimaryMedia` | `[FromBody]` |
| PATCH | `order` | `ReorderMedia` | `[FromBody]` |

## زیرساخت

- **ذخیره‌سازی شیئی S3**: تنها پیاده‌سازی `IStorageService` کلاس `S3FileStorageService` است و پروایدر
  محلی روی دیسک وجود ندارد. DI بر اساس `StorageOptions.Provider` یکی از مقادیر `s3`, `aws`, `arvan`,
  `liara`, `minio` را می‌پذیرد (همه به همان پیاده‌سازی S3 می‌رسند) و مقدار دیگر `NotSupportedException`
  می‌دهد. تنظیمات بخش `Storage`: `BucketName`, `BaseUrl`, `Endpoint`, `AccessKey`, `SecretKey`, `Region`,
  `ForcePathStyle` (پیش‌فرض `true`), `UseHttp` و `MaxFileSizeBytes` (پیش‌فرض ۱۰ مگابایت).
- **مسیر کلید**: `{folder}/{Guid}/{fileName}` که `folder` همان `EntityType` با حروف کوچک است.
- **اعتبارسنجی محتوا دو مرحله دارد**: ابتدا `IFileMagicBytesValidator` که امضای بایتی JPEG/PNG/GIF/WebP را
  می‌سنجد و در عدم تطابق خطای فارسی «محتوای فایل با نوع اعلام‌شده مطابقت ندارد.» با رویداد امنیتی
  `MaliciousUploadDetected` می‌دهد؛ سپس `IFileScanningService` که پیاده‌سازی پیش‌فرض آن
  `ClamAvFileScanningService` (پروتکل INSTREAM روی TCP، بخش `Storage:Antivirus` با `IsEnabled=false`)
  و جایگزین آن `NullFileScanningService` است.
- **حالت دسترسی فایل** با فلگ `FeatureFlags.StoragePresignedUrlEnabled` تعیین می‌شود: روشن → `Private` و
  استفاده از URL امضاشده؛ خاموش → `PublicRead`. `IStorageService.GetPresignedUrlAsync` هم در دسترس است.
  جریان کامل فایل به‌صورت بافر در حافظه (`MemoryStream`) انجام و سپس آپلود می‌شود.
- نگاشت EF: `Path` و `Size` به‌صورت نوع مالک‌شده، ایندکس `(EntityType, EntityId)` و
  `(IsDeleted, DeletedAt)`، **فیلتر سراسری `!e.IsDeleted`** و سقف ۵۰۰ کاراکتر برای `AltText`.
- `OrphanedFileCleanupJob` هر ۱۲ ساعت (قفل `jobs:orphaned-file-cleanup`، دسته‌های ۱۰۰) ردیف‌های
  حذف‌نرم‌شده قدیمی‌تر از ۲۴ ساعت را از ذخیره‌سازی و دیتابیس به‌طور فیزیکی پاک می‌کند.

## نکات و محدودیت‌ها

- **پردازش تصویر وجود ندارد**: نه تغییر اندازه، نه ساخت بندانگشتی و نه کتابخانه‌ای مانند ImageSharp در کد
  نیست؛ فایل فقط ذخیره می‌شود.
- سه سقف حجم متفاوت در مسیر آپلود دیده می‌شود: Validator و `RequestSizeLimit` مقدار ۱۰ مگابایت،
  `StorageOptions.MaxFileSizeBytes` مقدار ۱۰ مگابایت و `FileSize` در SharedKernel سقف ۱۰۰ مگابایت.
- `MediaDomainService.MaxMediaPerEntity = 20` هیچ‌جا اعمال نمی‌شود؛ سقف تعداد رسانه هر موجودیت فعال نیست.
- دریافت رسانه `GET media/{entityType}/{entityId}` عمومی است و فیلتر وضعیت/حذف به منطق Query سپرده شده است.
- پوشه `Infrastructure/Media/BackgroundServices/` خالی است؛ Job واقعی در
  `Infrastructure/BackgroundJobs/OrphanedFileCleanupJob.cs` قرار دارد.

## اسناد مرتبط

- لوگوی برند و اعتبارسنجی دو‌لایه آن: [brands.md](brands.md)
- محصول و رسانه‌های آن: [products.md](products.md)
- Jobها، فلگ‌های FeatureManagement و Health Checkها: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)،
  [01-getting-started/configuration.md](../01-getting-started/configuration.md)
