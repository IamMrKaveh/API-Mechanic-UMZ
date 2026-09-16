[بازگشت به فهرست ماژول‌ها](README.md)

# تیکت‌های پشتیبانی (Support)

ماژول Support چرخه تیکت را مدیریت می‌کند: موضوع، دسته، اولویت، وضعیت، پیام‌های مشتری و کارشناس و
قاعده دسترسی به تیکت. Application آن سه Command و چهار Query دارد و ۹ اکشن در دو کنترلر (کاربر و ادمین)
ارائه می‌کند. اعلان پاسخ تیکت در [notifications.md](notifications.md) مستند شده است.

## دامنه (`Domain/Support`)

`Ticket : AggregateRoot<TicketId>, IAuditable` با `CustomerId`, `AssignedAgentId?`, `Subject`, `Status`,
`Priority`, `Category`, `ResolvedAt?`, `LastActivityAt`, `MessageCount` و کمکی‌های
`IsClosed`, `IsOpen`, `IsAwaitingReply`, `IsAnswered`.

| متد | قاعده/اثر |
|---|---|
| `Open(id, customerId, subject, category, priority?)` | وضعیت `Open`، اولویت پیش‌فرض `Normal`؛ صدور `TicketCreatedEvent` |
| `AddMessage(messageId, senderId, senderType, content, now)` | تیکت بسته قابل پیام‌گذاری نیست («تیکت بسته شده است.»)؛ افزودن پیام و **گذار خودکار وضعیت** (پیام مشتری در `AwaitingReply` → `Open`، پیام کارشناس در `Open` و `Answered` → `AwaitingReply`)؛ صدور `TicketMessageAddedEvent` |
| `Close()` | وضعیت `Closed`؛ صدور `TicketClosedEvent` و `TicketStatusChangedEvent`؛ idempotent |
| `IsHighPriority` / `IsUrgent` / `RequiresUrgentAttention(now)` / `GetTimeToFirstResponse()` | کمکی‌های اولویت و زمان پاسخ |

Entity `TicketMessage : Entity<TicketMessageId>` با `TicketId`, `SenderId`, `SenderType`, `Content`,
`IsEdited`, `EditedAt?`, `SentAt` — متد ویرایش پیام در کد پیاده نشده است.

### ValueObject و Enumها

| نوع | مقادیر |
|---|---|
| `TicketStatus` | `Open` («باز»، نیازمند پاسخ)، `AwaitingReply` («در انتظار پاسخ»، نیازمند پاسخ)، `Answered` («پاسخ داده شده»)، `Closed` («بسته شده») — `FromString` برای مقدار ناشناس `Open` برمی‌گرداند |
| `TicketPriority` | `Low`, `Normal`, `High`, `Urgent` |
| `TicketCategory` | متن آزاد (الزام غیرخالی) |
| `TicketMessageSenderType` | `Customer = 1`, `Agent = 2`, `System = 3` |

نتایج دامنه: `TicketAccessResult` (`Allowed`/`Denied`) و `TicketMessageResult` (`Success`/`Failed`).
سرویس دامنه `TicketDomainService.ValidateUserAccess(ticket, userId, isAdmin)`: ادمین همیشه مجاز؛
کاربر عادی فقط اگر `CustomerId` یا `AssignedAgentId` باشد، وگرنه «شما دسترسی به این تیکت را ندارید.»

Domain Eventها (۵): `TicketCreatedEvent`, `TicketMessageAddedEvent`, `TicketAnsweredEvent`,
`TicketClosedEvent`, `TicketStatusChangedEvent` — **`TicketAnsweredEvent` هیچ‌جا صادر نمی‌شود**،
هرچند Handlerی برای آن ثبت شده است. پوشه `Exceptions/` وجود ندارد.

## Application (`Application/Support`)

**۳ Command:** `CreateTicket` (Validator: موضوع غیرخالی و حداکثر ۲۰۰، دسته غیرخالی، پیام غیرخالی و حداکثر
۵۰۰۰)، `ReplyToTicket`، `CloseTicket`.

**۴ Query:** `GetTickets`, `GetTicket`, `GetTicketDetails`, `GetAdminTickets`.
`GetTicketQuery` وجود دارد اما هیچ کنترلری از آن استفاده نمی‌کند.

**۱ Event Handler:** `TicketAnsweredEventHandler` روی `TicketAnsweredEvent` اعلان «پاسخ جدید به تیکت» با
نوع `TicketReply` و لینک `/dashboard/tickets/{id}` می‌سازد و رویداد حسابرسی ثبت می‌کند؛ خطاها به
حسابرسی منتقل می‌شوند.

نکات Handlerها: `CreateTicketHandler` تیکت را با `Ticket.Open` و نخستین پیام مشتری می‌سازد؛
`ReplyToTicketHandler` اگر فرستنده ادمین نباشد و مالک تیکت هم نباشد پاسخ Forbidden «دسترسی ممنوع.»
می‌دهد و نوع فرستنده را از `currentUser.IsAdmin` تعیین می‌کند؛ `CloseTicketHandler` نیز همین بررسی
دسترسی را دارد.

## API

جمع: **۲ کنترلر و ۹ اکشن**. قراردادهای عمومی در [05-api/conventions.md](../05-api/conventions.md).

`TicketsController` — `[Route("api/v{version:apiVersion}/tickets")]` — `[Authorize]`؛ ۵ اکشن:

| متد | مسیر | اکشن |
|---|---|---|
| GET | — | `GetMyTickets` |
| GET | `{id:guid}` | `GetTicketDetails` |
| POST | — | `CreateTicket` |
| POST | `{id:guid}/replies` | `ReplyToTicket` |
| PATCH | `{id:guid}/status` | `CloseTicket` |

`AdminTicketsController` — `[Route("api/v{version:apiVersion}/admin/tickets")]` — `[Authorize(Roles = "Admin")]`؛ ۴ اکشن:
`GET` → `GetTickets`، `GET {id:guid}` → `GetTicketDetails`،
`POST {id:guid}/replies` → `ReplyToTicket`، `PATCH {id:guid}/status` → `CloseTicket`.

هیچ اتریبیوت Rate Limit در این ماژول وجود ندارد.

## زیرساخت

- نگاشت EF در `TicketConfiguration` و `TicketMessageConfiguration`؛ `TicketRepository` نوشتن و
  `TicketQueryService` خواندن را انجام می‌دهد. `TicketQueryService` مقدار `pageSize` را در بازه ۱ تا ۲۰۰
  محدود و پیش‌فرض ۲۰ می‌کند و بر اساس وضعیت/اولویت/کاربر فیلتر می‌گذارد.
- پوشه `Features/Shared/NewFolder/` در Application خالی است (باقی‌مانده ساختار، بدون محتوا).
- بدون کش و بدون Job اختصاصی.

## نکات و محدودیت‌ها

- وضعیت `Answered` در گذارهای Aggregate **هرگز تنظیم نمی‌شود**؛ تنها راه ورود به آن، به‌روزرسانی
  مستقیم داده یا Queryهای بیرونی است و در کد فعلی مشخص نیست چه مسیری آن را می‌نویسد.
- `TicketAnsweredEvent` و در نتیجه `TicketAnsweredEventHandler` در عمل بی‌اثرند، چون رویداد صادر نمی‌شود.
- متد ویرایش پیام (`IsEdited`/`EditedAt`) در دامنه پیاده نشده؛ فقط فیلدها وجود دارند.
- `GetTicketQuery` بدون مصرف‌کننده در API است (فقط توسط Handler خودش و تست‌ها ارجاع داده می‌شود).
- هیچ سقف نرخ یا سقف تعداد پیام برای تیکت‌ها تعریف نشده است.

## اسناد مرتبط

- ساخت و مصرف اعلان‌ها: [notifications.md](notifications.md)
- کاربران و احراز هویت: [identity/users.md](identity/users.md)، [identity/auth-security.md](identity/auth-security.md)
- حسابرسی رویدادهای پشتیبانی: [audit.md](audit.md)
