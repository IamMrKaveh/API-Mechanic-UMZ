[بازگشت به فهرست ماژول‌ها](../README.md)

# کاربران و آدرس‌ها (Users)

ماژول `User` هویت کاربر، پروفایل، قفل حساب پس از تلاش‌های ناموفق و دفترچه آدرس‌ها را مدیریت می‌کند.
Aggregate اصلی `User` چهارده Domain Event و سه Exception دارد و Application آن سیزده Command و شش Query
را در پنج کنترلر ارائه می‌دهد. ورود، OTP و سشن‌ها در [auth-security.md](auth-security.md) مستند شده‌اند.

## دامنه (`Domain/User`)

`User : AggregateRoot<UserId>` با ثابت‌های `MaxFailedLoginAttempts = 5`، `LockoutDuration = 30 min` و
`MaxAddresses = 10`. فیلدهای اصلی: `FullName`, `Email`, `PhoneNumber?`, `PasswordHash`, `IsActive`,
`IsAdmin`, `IsEmailVerified`, `FailedLoginAttempts`, `LockoutEnd?`, `LastLoginAt?`, `DefaultAddressId?`
و مجموعه `Addresses`. ویژگی `IsLockedOut` از `LockoutEnd` محاسبه می‌شود.

| متد | قاعده/اثر |
|---|---|
| `Create` | ساخت کاربر با رمز هش‌شده؛ صدور `UserRegisteredEvent` |
| `CreateExternal` | کاربر بدون رمز (ورود گوگل) با ایمیل تأییدشده |
| `RegisterByPhone` | کاربر موقت با ایمیل ساخته‌شده توسط `Email.CreateTemp` و نام/رمز خالی |
| `UpdateProfile` | تغییر نام و شماره با `EnsureActive`؛ صدور `UserProfileUpdatedEvent` |
| `ChangePasswordHash` | ریست شمارنده خطا و `LockoutEnd`؛ صدور `UserPasswordChangedEvent` |
| `VerifyEmail` / `Activate` / `Deactivate` | تغییر وضعیت‌های ایمیل و فعال‌بودن با رویداد متناظر |
| `PromoteToAdmin` / `DemoteFromAdmin` | تغییر نقش ادمین (نیازمند کاربر فعال) |
| `RecordSuccessfulLogin` | بررسی قفل‌بودن، ریست شمارنده، ثبت `LastLoginAt` |
| `RecordFailedLogin` | در خطای پنجم `LockoutEnd = now + 30min` و صدور `UserLockedOutEvent` |
| `AddAddress` | الزام فعال‌بودن و سقف ۱۰ آدرس (`MaxAddresses`) |
| `UpdateAddress` / `RemoveAddress` | ویرایش و حذف آدرس؛ در حذف آدرس پیش‌فرض، اولین آدرس باقی‌مانده پیش‌فرض می‌شود |
| `SetDefaultAddress` | پیش‌فرض قبلی را لغو و آدرس جدید را پیش‌فرض می‌کند |
| `ChangePhoneNumber` | الزام وجود شماره قبلی و فعال‌بودن کاربر |

`UserAddress : Entity<UserAddressId>` (`Entities/UserAddress.cs`) با اعتبارسنجی: عنوان/گیرنده/استان/شهر/آدرس
الزامی با سقف طول ۱۰۰/۱۰۰/۵۰/۵۰/۵۰۰، کد پستی دقیقاً ۱۰ رقم و طول/عرض جغرافیایی در بازه مجاز.

### ValueObjectها

| ValueObject | قاعده |
|---|---|
| `UserId` / `UserAddressId` | شناسه strongly-typed بر پایه Guid |
| `PhoneNumber` | موبایل ایران؛ نرمال‌سازی ارقام فارسی و پیشوندهای `98`/`0098` و `09`؛ `Create` خطای `InvalidPhoneNumberException` می‌دهد |
| `FullName` | نام و نام خانوادگی حداکثر ۵۰ کاراکتر با حروف فارسی/انگلیسی (regex مشخص) |
| `Email` | در `SharedKernel` تعریف شده است، نه `Domain/User` |

### Exceptionها

| Exception | `ErrorCode` | قاعده |
|---|---|---|
| `InvalidPhoneNumberException` | `INVALID_PHONE_NUMBER` | شماره خالی یا نامعتبر |
| `UserAddressNotFoundException` | `USER_ADDRESS_NOT_FOUND` | آدرس درخواستی در کاربر وجود ندارد |
| `UserInactiveException` | `USER_INACTIVE` | عملیات روی کاربر غیرفعال (`EnsureActive`) |

### Domain Eventها (۱۴ رویداد در `Domain/User/Events`)

`UserRegisteredEvent`, `UserProfileUpdatedEvent`, `UserPasswordChangedEvent`, `UserEmailVerifiedEvent`,
`UserActivatedEvent`, `UserDeactivatedEvent`, `UserPromotedToAdminEvent`, `UserDemotedFromAdminEvent`,
`UserPhoneChangedEvent`, `UserAddressAddedEvent`, `UserAddressUpdatedEvent`, `UserAddressRemovedEvent`,
`UserAddressSetAsDefaultEvent`, `UserDefaultAddressChangedEvent`.

پوشه `Services/` این ماژول خالی است و کلاسی با نام `Rules` وجود ندارد.

## Application (`Application/User`)

**۱۳ Command:**

| گروه | Commandها |
|---|---|
| پروفایل و امنیت | `UpdateProfile`, `ChangePassword`, `ChangePhoneNumber`, `DeactivateAccount` |
| مدیریت ادمین | `CreateUser`, `UpdateUser`, `DeleteUser`, `RestoreUser`, `ChangeUserStatus`, `ChangeUserRole` |
| آدرس‌ها | `CreateUserAddress`, `UpdateUserAddress`, `DeleteUserAddress` |

سه Validator برای `ChangePassword`, `ChangePhoneNumber`, `UpdateProfile` وجود دارد. این ماژول
Event Handler اختصاصی ندارد؛ رویدادهای آن در سایر ماژول‌ها و لایه زیرساخت مصرف می‌شوند
(برای نمونه ساخت کیف پول در [auth-security.md](auth-security.md) و ابطال کش در
[02-architecture/cross-cutting.md](../../02-architecture/cross-cutting.md)).

**۶ Query:** `GetCurrentUser`, `GetUserById`, `GetUsers`, `GetAdminUsers`, `GetUserAddresses`, `GetUserDashboard`.

جریان‌های شاخص:

- `DeactivateAccountHandler` علاوه بر `user.Deactivate()` همه سشن‌ها را با
  `ISessionService.RevokeAllSessionsAsync` ابطال و رویداد امنیتی «AccountDeactivated» ثبت می‌کند.
- `ChangePhoneNumberHandler` یکتایی شماره را با `ExistsByPhoneNumberAsync` می‌سنجد، سپس OTP با
  هدف `OtpPurpose.PhoneVerification` را تأیید و در پایان شماره را تغییر می‌دهد.
- `IUserQueryService` سمت خواندن را تأمین می‌کند: پروفایل، فهرست صفحه‌بندی‌شده کاربران و ادمین‌ها،
  آدرس‌ها، سشن‌های فعال و داشبورد. `GetAdminUsersPagedAsync` آمار سفارش، مانده کیف پول، تعداد آدرس و
  تیکت‌های باز را با هم ترکیب می‌کند و `GetActiveSessionsAsync` برچسب دستگاه/مرورگر/پلتفرم را از
  User-Agent استخراج می‌کند.

## API

قالب نسخه‌دار `api/v{version:apiVersion}` و قراردادهای عمومی در
[05-api/conventions.md](../../05-api/conventions.md) آمده است. پنج کنترلر و ۲۲ اکشن:

`ProfileController` — `[Route("api/v{version:apiVersion}/profile")]` — `[Authorize]`:

| متد | مسیر | اکشن |
|---|---|---|
| GET | — | `GetProfile` |
| GET | `reviews` | `GetMyReviews` (با `[Obsolete]`؛ به ماژول Review واگذار شده است) |
| PUT | — | `UpdateProfile` |
| DELETE | — | `DeleteAccount` |
| PATCH | `password` | `ChangePassword` |
| PATCH | `phone` | `ChangePhoneNumber` |

`AddressController` — `[Route("api/v{version:apiVersion}/profile/addresses")]` — `[Authorize]`:
`GET` → `GetUserAddresses`، `POST` → `AddAddress`، `PUT {id:guid}` → `UpdateAddress`، `DELETE {id:guid}` → `DeleteAddress`.

`AdminUserController` — `[Route("api/v{version:apiVersion}/admin/users")]` — `[Authorize(Roles = "Admin")]`:
`GET` → `GetUsers`، `GET rich` → `GetAdminUsers`، `GET {id:guid}` → `GetUser`، `POST` → `CreateUser`،
`POST {id:guid}/restore` → `RestoreUser`، `PUT {id:guid}` → `UpdateUser`، `DELETE {id:guid}` → `DeleteUser`،
`PATCH {id:guid}/status` → `ChangeUserStatus`، `PATCH {id:guid}/role` → `ChangeUserRole`.

`UserController` — `[Route("api/v{version:apiVersion}/users")]` — `[Authorize]`:
`GET {id:guid}` → `GetUser`، `PUT {id:guid}` → `UpdateUser`.

`DashboardController` — `[Route("api/v{version:apiVersion}/dashboard")]` — `[Authorize]`:
`GET` → `GetDashboardSummary`.

جمع این ماژول: **۵ کنترلر و ۲۲ اکشن HTTP**. هیچ‌کدام اتریبیوت Rate Limit اختصاصی ندارند.

## زیرساخت

- `UserConfiguration` ایمیل را با ایندکس یگانه و شماره را با ایندکس یگانه فیلترشده
  (`WHERE "PhoneNumber" IS NOT NULL`) نگه می‌دارد؛ `PasswordHash` سقف ۵۰۰ کاراکتر و
  `HasQueryFilter(e => e.IsActive)` دارد، بنابراین **حذف نرم کاربر با غیرفعال‌سازی (`IsActive=false`)**
  پیاده شده است.
- `UserAddressConfiguration` شماره گیرنده را در ستون `ReceiverPhoneNumber` نگه می‌دارد، حذف آبشاری از
  کاربر دارد و فیلتر `e => e.User.IsActive` اعمال می‌کند؛ طول/عرض جغرافیایی `decimal(9,6)` است.
- `UserQueryService` (`Infrastructure/User/QueryServices/`) تمام Queryهای خواندنی را با projection
  مستقیم انجام می‌دهد و از `IUserRepository` برای نوشتن استفاده می‌شود.

## نکات و محدودیت‌ها

- قفل حساب با پنج خطای پی‌درپی و مدت ۳۰ دقیقه در Domain هاردکد شده است؛
  `AuthOptions.MaxFailedLoginAttempts` و `LockoutDurationMinutes` مقادیر مشابهی دارند اما مسیر واقعی
  ورود کاربر، OTP است و این شمارنده‌ها را مصرف نمی‌کند (ورود با رمز عبور در API وجود ندارد).
- `UserRepository.GetAllActiveUserIdsAsync` برای ارسال اعلان گروهی استفاده می‌شود
  (به [notifications.md](../notifications.md) مراجعه کنید).
- سقف آدرس هر کاربر ۱۰ عدد است و در Domain اعمال می‌شود؛ سقف مشابه در لایه Application تکرار نشده است.
- `DeactivateAccount` از سمت کاربر و `DeleteUser` از سمت ادمین هر دو به غیرفعال‌سازی می‌رسند؛
  حذف فیزیکی کاربر در کد وجود ندارد.

## اسناد مرتبط

- ورود، OTP، سشن و گوگل: [auth-security.md](auth-security.md)
- سفارش‌های کاربر و آمار داشبورد: [orders.md](../orders.md)، [analytics.md](../analytics.md)
- نظرهای کاربر و اعلان‌ها: [reviews.md](../reviews.md)، [notifications.md](../notifications.md)
- قواعد سراسری API: [05-api/conventions.md](../../05-api/conventions.md)
