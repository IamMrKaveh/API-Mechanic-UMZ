[بازگشت به فهرست ماژول‌ها](README.md)

# لاگ حسابرسی و آرشیو (Audit)

ماژول Audit رویدادهای حساس سیستم را با هش یکپارچگی ذخیره می‌کند، داده‌های حساس را ماسک می‌کند،
لاگ‌های قدیمی را بر اساس نوع به فایل/آبجکت‌استوریج آرشیو می‌کند و امکان بازبینی و برون‌بری آن‌ها را
می‌دهد. Application آن پنج Query دارد (بدون Command) و ۶ اکشن مدیریتی در یک کنترلر ارائه می‌کند.
سخت‌سازی و نگهداری اسرار در [07-operations/security.md](../07-operations/security.md) آمده است.

## دامنه (`Domain/Audit`)

`AuditLog : AggregateRoot<AuditLogId>` با `CurrentHashVersion = 2` و فیلدهای `UserId?`, `EventType`,
`Action`, `Details?`, `IpAddress`, `UserAgent?`, `EntityType?`, `EntityId?`, `CreatedAt`
(UTC با دقت میکروثانیه), `IntegrityHash` (SHA256 به Base64), `HashVersion`, `IsArchived`, `ArchivedAt?`.

| متد | نقش |
|---|---|
| `Create` | ساخت لاگ و صدور `AuditLogCreatedEvent` |
| `MarkAsArchived` | علامت‌گذاری آرشیوشده (بدون حذف ردیف) |
| `VerifyIntegrity` | بازمحاسبه و مقایسه هش فعلی |
| `RecomputeIntegrityHash` / `UpgradeHashVersion` | بازسازی هش و ارتقای نسخه |
| `ComputeHash` (خصوصی) | نسخه ۱: `userId|EventType|Action|Details|IpAddress|CreatedAt`؛ نسخه ۲: افزودن `EntityType`, `EntityId`, `UserAgent` |

`AuditEventType` مجموعه‌ای ثابت از نوع‌هاست: `Authentication`, `SecurityEvent`, `OrderEvent`,
`PaymentEvent`, `InventoryEvent`, `ProductEvent`, `AdminEvent`, `SystemEvent`, `Error`, `Warning`,
`Information`, `Debug`.

Domain Event (۱): `AuditLogCreatedEvent(AuditLogId, string Action)` — **هیچ Handlerی برای آن ثبت نشده است**.
قراردادها: `IAuditRepository` (`AddAuditLogAsync`, `GetByIdAsync`, `GetForArchiveAsync`, `RemoveRangeAsync`)
و `IAuditArchiveStorage` (`ArchiveAsync(logs, label, timestamp, ct)`).

## چه چیزی حسابرسی می‌شود

- حسابرسی **صریح** از طریق `IAuditService` (`Application/Audit/Contracts/`) انجام می‌شود؛ متدها:
  `LogAsync`, `LogInformationAsync`, `LogDebugAsync`, `LogWarningAsync`, `LogErrorAsync`,
  `LogSecurityEventAsync`, `LogSystemEventAsync`, `LogOrderEventAsync`, `LogPaymentEventAsync`,
  `LogInventoryEventAsync`, `LogProductEventAsync`, `LogAdminEventAsync`. صدازنندگان در سرویس‌ها،
  Handlerها و Jobهای سراسر برنامه پخش شده‌اند.
- **حسابرسی خودکار تغییر موجودیت وجود ندارد**: `AuditableEntityInterceptor` فقط `CreatedAt`/`UpdatedAt` و
  توکن همزمانی را ست می‌کند و `DomainEventInterceptor` رویدادها را به Outbox می‌نویسد؛ هیچ‌کدام ردیف
  `AuditLog` نمی‌سازند. یعنی «چه کسی چه فیلدی را عوض کرد» به‌صورت خودکار ثبت نمی‌شود.
- **ماسک داده حساس**: `AuditMaskingService` پیش از ذخیره `Details`، توکن Bearer، شماره کارت ۱۶ رقمی،
  موبایل ایرانی و ایمیل را با regex ماسک می‌کند و متدهای `MaskPhoneNumber`, `MaskEmail`,
  `MaskIpAddress` (دو اکتت اول) را فراهم می‌کند.

## Application (`Application/Audit`)

پوشه Commands وجود ندارد. **۵ Query:** `GetAuditLogs` (فیلترهای کاربر، نوع رویداد، نوع/شناسه موجودیت،
اکشن، کلیدواژه، IP و بازه زمانی با صفحه‌بندی پیش‌فرض ۵۰ و مرتب‌سازی `CreatedAt` نزولی),
`GetAuditLogById`, `GetAuditStatistics`, `VerifyAuditIntegrity`, `ExportAuditLogs`
(قالب‌های `csv` و `json` با سقف `MaxRows`). هر پنج Query Validator دارند.

## API

جمع: **۱ کنترلر و ۶ اکشن**، همه `GET` و مدیریتی. قراردادهای عمومی در
[05-api/conventions.md](../05-api/conventions.md).

`AdminAuditLogsController` — `[Route("api/v{version:apiVersion}/admin/audit-logs")]` —
`[Authorize(Roles = "Admin")]` و `[Tags("Admin - Audit Logs")]`:

| متد | مسیر | اکشن |
|---|---|---|
| GET | — | `GetAuditLogs` |
| GET | `{id:guid}` | `GetById` |
| GET | `{id:guid}/integrity` | `VerifyIntegrity` |
| GET | `statistics` | `GetStatistics` |
| GET | `export/csv` | `ExportCsv` |
| GET | `export/json` | `ExportJson` |

## زیرساخت

- `AuditService` (`Infrastructure/Audit/Services/`) جزئیات را ماسک می‌کند، `AuditLog` می‌سازد و با
  `IUnitOfWork` ذخیره می‌کند؛ خطاها را می‌بلعد و لاگ می‌کند تا حسابرسی باعث شکست عملیات اصلی نشود.
- نگاشت EF: جدول `AuditLogs`، سقف طول `EventType` ۱۰۰، `Action` ۲۰۰، `IpAddress` ۴۵، `UserAgent` ۵۰۰ و
  `IntegrityHash` ۲۰۰؛ ستون زمان `timestamp(6) with time zone`؛ ایندکس‌های `UserId`, `CreatedAt`,
  `EventType`, `HashVersion`, `EntityType`, `IsArchived` و ترکیب‌های
  `(CreatedAt, IsArchived, EventType)` و `(EntityType, EntityId)`.
- **آرشیو** دو پیاده‌سازی دارد: `FileSystemAuditArchiveStorage` (خروجی gzip JSON در مسیر
  `Audit:ArchivePath`، پیش‌فرض `{AppContext.BaseDirectory}\audit_archives` با ساختار
  `yyyy\yyyy-MM-dd\{label}_{timestamp}_{guid}.json.gz`) و `S3AuditArchiveStorage` (کلید
  `audit-archives/{year}/{yyyy-MM-dd}/...` با `BucketName` از `S3Options`). انتخاب با
  `Storage:AuditArchive:Provider == "S3"` انجام می‌شود.
- **نگهداشت و پاک‌سازی** با `AuditRetentionJob` (روزانه، قفل `jobs:audit-retention`): لاگ‌های معمولی
  قدیمی‌تر از **۹۰ روز** آرشیو و حذف می‌شوند (دسته‌های ۱۰۰۰)؛ انواع امنیتی
  (`SecurityEvent, UserAction, AdminEvent, AuthEvent, LoginEvent`) قدیمی‌تر از **۲ سال (۷۳۰ روز)** و
  انواع مالی (`PaymentEvent, OrderEvent, RefundEvent, FinancialEvent, TransactionEvent`) قدیمی‌تر از
  **۷ سال (۲۵۵۵ روز)** آرشیو می‌شوند — مالی‌ها پس از آرشیو با `MarkAsArchived` در دیتابیس باقی می‌مانند.
- **ارتقای هش** با `AuditHashUpgradeJob` (قفل `jobs:audit-hash-upgrade`، دسته‌های ۵۰۰، تأخیر بیکاری
  ۶ ساعت): ردیف‌های `HashVersion < 2` را به نسخه ۲ ارتقا و هش را بازمحاسبه می‌کند.
- **اعتبارسنجی یکپارچگی** با `VerifyAuditIntegrity` (Endpoint مدیریتی) یا متد `VerifyIntegrity` روی
  Aggregate انجام می‌شود.

## نکات و محدودیت‌ها

- `AuditLogCreatedEvent` بدون مصرف‌کننده است؛ هیچ واکنش زنجیره‌ای به ثبت لاگ وجود ندارد.
- لاگ‌های مالی پس از ۷ سال از دیتابیس حذف نمی‌شوند (فقط علامت آرشیو می‌خورند)؛ نگهداشت آن‌ها به حجم
  جدول وابسته است.
- چون حسابرسی خودکار نیست، پوشش واقعی به فراخوانی‌های دستی `IAuditService` در هر ماژول بستگی دارد؛
  در نتیجه برخی تغییرات دامنه بدون ردیف حسابرسی می‌مانند.
- برون‌بری CSV/JSON با سقف `MaxRows` محدود می‌شود؛ برای حجم بالاتر مسیر دیگری (Job/Batch) وجود ندارد.
- آرشیو به فایل یا S3 می‌رود و **بازخوانی/بازیابی آرشیو در کد پیاده نشده است**؛ داده آرشیوشده از مسیر
  API قابل مشاهده نیست.

## اسناد مرتبط

- DataProtection، gitleaks و نگهداری کلیدها: [07-operations/security.md](../07-operations/security.md)
- Outbox و Interceptorها: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
- رویدادهای مالی و امنیتی ماژول‌ها: [wallet.md](wallet.md)، [payments.md](payments.md)،
  [identity/auth-security.md](identity/auth-security.md)
