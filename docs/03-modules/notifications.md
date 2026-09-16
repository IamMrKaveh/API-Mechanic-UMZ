[بازگشت به فهرست ماژول‌ها](README.md)

# اعلان‌ها و پیامک (Notifications)

ماژول Notification اعلان‌های درون‌برنامه‌ای (in-app) را ذخیره و مدیریت می‌کند: عنوان، متن، نوع، لینک
اقدام، وضعیت خوانده‌شده و ارتباط با موجودیت مرتبط. Application آن پنج Command و سه Query دارد و ۸ اکشن
در دو کنترلر کاربری و مدیریتی ارائه می‌کند. ارسال پیامک OTP در همین سند به‌عنوان بخش ارتباطات آمده است.

## دامنه (`Domain/Notification`)

`Notification : AggregateRoot<NotificationId>, IAuditable` با `Title`, `Message`, `Type`, `ActionUrl?`,
`RelatedEntityType?`, `RelatedEntityId?`, `IsRead`, `UserId`.

| متد | قاعده/اثر |
|---|---|
| `Create` | عنوان («عنوان اعلان الزامی است.») و متن («متن اعلان الزامی است.») الزامی؛ `IsRead = false`؛ صدور `NotificationCreatedEvent` |
| `MarkAsRead` | idempotent؛ صدور `NotificationReadEvent` |
| `EnsureUserAccess(userId)` | دسترسی کاربر دیگر ممنوع («شما دسترسی به این اعلان را ندارید.») |

خانگارها: **فقط درون‌برنامه‌ای**. هیچ وضعیت تحویل per-channel یا صف ارسال در دامنه وجود ندارد.

`NotificationType` دوازده نوع ثابت دارد؛ هر نوع `Value`, `DisplayName` فارسی, `Icon`, `Color` و
`Category` دارد:

| دسته | انواع |
|---|---|
| `Order` | `OrderCreated`, `OrderPaid`, `OrderShipped`, `OrderDelivered`, `OrderCancelled` |
| `Support` | `TicketReply` |
| `Product` | `PriceDropAlert`, `StockAlert` |
| `Marketing` | `DiscountCode` |
| `System` | `SystemAlert`, `SecurityAlert`, `AccountUpdate` |

علاوه بر این‌ها، `Custom(value, displayName, icon, color)` و `FromString` (مقدار ناشناس → `SystemAlert`)
وجود دارد. `NotificationPriority` (`Low`, `Normal`, `High`, `Urgent`) تعریف شده اما Aggregate از آن
استفاده نمی‌کند. Enum `NotificationCategory`: `Order, Support, Product, Marketing, System, Custom`.

Domain Eventها (۲): `NotificationCreatedEvent`, `NotificationReadEvent`.
پوشه‌های `Exceptions` و `Services` خالی‌اند.

## Application (`Application/Notification`)

**۵ Command:** `MarkNotificationRead`, `MarkAllNotificationsRead`, `DeleteNotification`,
`AdminSendNotification`, `AdminDeleteNotification`.

**۳ Query:** `GetNotifications` (با `UnreadOnly`, `Page`, `PageSize`), `GetAllNotifications`,
`GetUnreadNotificationCount`.

`AdminSendNotificationHandler` با `SendToAll = true` اعلان را برای همه کاربران فعال
(`IUserRepository.GetAllActiveUserIdsAsync`) می‌سازد و در غیر این صورت `UserId` را الزامی می‌کند
(«شناسه کاربر الزامی است.»).

**این ماژول Event Handler داخلی ندارد**؛ برعکس، ماژول‌های دیگر اعلان می‌سازند. تولیدکنندگان اعلان در کد:

| تولیدکننده | رویداد/مسیر |
|---|---|
| `OrderPlacedNotificationEventHandler` (Order) | `OrderCreatedEvent` |
| `UpdateOrderStatusHandler`, `RequestReturnHandler` (Order) | تغییر وضعیت و مرجوعی |
| `TicketAnsweredEventHandler` (Support) | `TicketAnsweredEvent` |
| هفت Handler ماژول Wallet | شارژ/بدهکار/درخواست بدهکار/تجمید/بازکردن/برداشت پرداخت‌شده/برداشت رد‌شده |
| `PaymentSucceededNotificationEventHandler` (Payment، لایه زیرساخت) | `PaymentSucceededEvent` |

قراردادها: `INotificationService` (`CreateNotificationAsync`, `MarkAsReadAsync`, `MarkAllAsReadAsync`,
`DeleteAsync`, `SendOrderStatusNotificationAsync`) و `INotificationQueryService`.

## API

جمع: **۲ کنترلر و ۸ اکشن**. قراردادهای عمومی در [05-api/conventions.md](../05-api/conventions.md).

`NotificationsController` — `[Route("api/v{version:apiVersion}/notifications")]` — `[Authorize]`؛ ۵ اکشن:

| متد | مسیر | اکشن | دسترسی اکشن |
|---|---|---|---|
| GET | — | `GetMyNotifications` | سطح کلاس |
| GET | `unread-count` | `GetUnreadCount` | **`[AllowAnonymous]`** |
| PATCH | `{id:guid}/read` | `MarkAsRead` | سطح کلاس |
| PATCH | `read` | `MarkAllAsRead` | سطح کلاس |
| DELETE | `{id:guid}` | `DeleteNotification` | سطح کلاس |

`AdminNotificationController` — `[Route("api/v{version:apiVersion}/admin/notifications")]` — `[Authorize(Roles = "Admin")]`؛ ۳ اکشن:
`GET` → `GetAll`، `POST send` → `Send`، `DELETE {id:guid}` → `Delete`.

اکشن `GET notifications/unread-count` با `[AllowAnonymous]` باز است؛ در نبود کاربر، مقدار شمارش بر پایه
هویت خالی محاسبه می‌شود.

## زیرساخت و ارتباطات

- `NotificationService` (`Infrastructure/Notification/Services/`) پیاده‌سازی `INotificationService` است و
  `SendOrderStatusNotificationAsync` وضعیت سفارش را به نوع اعلان نگاشت می‌کند:
  `Paid→OrderPaid`, `Shipped→OrderShipped`, `Delivered→OrderDelivered`, `Cancelled→OrderCancelled` و
  بقیه → `OrderCreated`.
- `NotificationQueryService` صفحه‌بندی با `AsNoTracking` انجام می‌دهد و **کشی ندارد**؛ Job پس‌زمینه هم برای
  پاک‌سازی اعلان‌ها وجود ندارد.
- **پیامک (Communication):** قرارداد `ISmsService` در `Application/Communication/Contracts/` تنها یک متد
  دارد: `SendOtpSMSAsync(PhoneNumber, OtpCode, CancellationToken)`. پیاده‌سازی واقعی
  `Infrastructure/Communication/Services/SmsService.cs` روی **Kavenegar** است و Endpoint
  `verify/lookup.json` را با `receptor`/`token`/`template` صدا می‌زند. تنظیمات `KavenegarOptions`
  (بخش `Kavenegar`): `ApiKey`, `Sender`, `OtpTemplate` (پیش‌فرض `"verify"`) — همه الزامی.
  `KavenegarHealthCheck` با `account/info.json` سلامت سرویس را می‌سنجد (مهلت ۵ ثانیه، کش ۶۰ ثانیه) و
  خرابی آن وضعیت `Degraded` می‌دهد.
- **ایمیل و SignalR وجود ندارد**: در کد نه قرارداد ایمیل/SMTP هست و نه Hub/`MapHub`.

## نکات و محدودیت‌ها

- ارسال پیامک فقط برای OTP است؛ اعلان‌های سفارش/کیف پول/تیکت هیچ کانال پیامکی یا ایمیلی ندارند و
  صرفاً در دیتابیس ذخیره می‌شوند (کاربر باید خودش اعلان‌ها را بخواند).
- `NotificationPriority` بدون مصرف در Aggregate است؛ اولویت واقعی اعلان‌ها در کد تعیین نمی‌شود.
- `AdminSendNotification` با `SendToAll` برای هر کاربر یک ردیف اعلان می‌سازد؛ برای تعداد بالای کاربران
  هیچ مسیر دسته‌ای (batch) یا Job پس‌زمینه‌ای وجود ندارد.
- هیچ Endpoint یا سرویسی برای پاک‌سازی/نگهداشت اعلان‌های قدیمی وجود ندارد.
- شمارش اعلان خوانده‌نشده در `unread-count` ناشناس باز است؛ در نبود هویت، رفتار دقیق پاسخ در کد فعلی
  مشخص نیست.

## اسناد مرتبط

- پیامک OTP و Kavenegar در جریان ورود: [identity/auth-security.md](identity/auth-security.md)
- اعلان سفارش و کیف پول: [orders.md](orders.md)، [wallet.md](wallet.md)
- اعلان تیکت: [support.md](support.md)
- Health Checkها: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
