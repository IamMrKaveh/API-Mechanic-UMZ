[بازگشت به فهرست ماژول‌ها](../README.md)

# احراز هویت و امنیت ورود (Auth & Security)

ورود با OTP پیامکی، ورود با گوگل، صدور و چرخش Access/Refresh Token و مدیریت سشن‌های کاربر
در این ماژول متمرکز است. Domain آن (`Domain/Security`) دو Aggregate مستقل `UserSession` و `UserOtp`
دارد و Application آن (`Application/Auth`) نُه Command و دو Query را در بر می‌گیرد.
هویت کاربر و آدرس‌ها موضوع سند [users.md](users.md) است؛ سخت‌سازی عملیاتی و نگهداری اسرار
در [07-operations/security.md](../../07-operations/security.md) آمده و در این سند تکرار نمی‌شود.

## دامنه (`Domain/Security`)

| نوع | نام | نقش |
|---|---|---|
| Aggregate | `UserSession` | یک ورود به ازای هر دستگاه؛ نگهدارنده Refresh Token و وضعیت ابطال |
| Aggregate | `UserOtp` | کد OTP هش‌شده با شمارش تلاش‌های ناموفق و انقضا |
| ValueObject | `SessionId` / `OtpId` | شناسه‌های strongly-typed بر پایه Guid |
| ValueObject | `RefreshToken` | طول ۳۲ تا ۵۱۲ کاراکتر؛ `Generate()` = ۶۴ بایت تصادفی Base64 و مقایسه ترتیبی |
| ValueObject | `OtpCode` | دقیقاً ۶ رقم؛ تولید با `RandomNumberGenerator` و هش SHA256؛ مقایسه با `FixedTimeEquals` |
| ValueObject | `DeviceInfo` | حداکثر ۵۰۰ کاراکتر؛ مقدار خالی به `Unknown` تبدیل می‌شود |
| Enum | `OtpPurpose` | `EmailVerification`, `PasswordReset`, `PhoneVerification`, `TwoFactorAuthentication`, `Login` |
| Enum | `SessionRevocationReason` | `UserRequested`, `AdminRevoked`, `SecurityConcern`, `PasswordChanged`, `AccountDeactivated`, `Expired`, `AllSessionsRevoked`, `PhoneChanged` |
| Enum | `OtpRateLimitStatus` | `Allowed`, `Blocked`, `TemporarilyBlocked` |

قواعد کلیدی:

- `UserSession.Create` انقضای آینده و حداکثر `MaxSessionDurationDays = 90` روز را الزام می‌کند؛
  `IsActive` ترکیب «ابطال‌نشده و منقضی‌نشده» است و `Revoke` پس از ابطال، بی‌اثر (no-op) می‌شود.
- `UserOtp.Create` اعتبار حداکثر ۳۰ دقیقه و `MaxVerificationAttempts = 5` را اعمال می‌کند؛
  عبور از سقف تلاش، خطای `OtpMaxAttemptsExceededException` می‌دهد.
- پوشه‌های `Rules/`، `Services/`، `Specifications/` و `Results/` این ماژول خالی‌اند؛ قاعده‌ای به‌صورت کلاس جدا وجود ندارد.

### Exceptionها

| Exception | `ErrorCode` | قاعده |
|---|---|---|
| `InvalidOtpCodeException` | `INVALID_OTP_CODE` | کد وارد‌شده با هش ذخیره‌شده مطابق نیست |
| `OtpAlreadyVerifiedException` | `OTP_ALREADY_VERIFIED` | تأیید مجدد یک OTP مصرف‌شده |
| `OtpExpiredException` | `OTP_EXPIRED` | تأیید پس از `ExpiresAt` |
| `OtpMaxAttemptsExceededException` | `OTP_MAX_ATTEMPTS_EXCEEDED` | عبور از ۵ تلاش ناموفق |
| `SessionExpiredException` | `SESSION_EXPIRED` | استفاده از سشن منقضی |

### Domain Eventها (۱۱ رویداد در `Domain/Security/Events`)

| رویداد | معنی |
|---|---|
| `SessionCreatedEvent` | سشن جدید ساخته شد (کاربر، دستگاه، IP، انقضا) |
| `SessionRevokedEvent` | سشن با دلیل مشخص ابطال شد |
| `SessionExpiredEvent` | سشن منقضی علامت خورد |
| `AllSessionsRevokedEvent` | همه سشن‌های کاربر ابطال شد (تعداد ابطال‌شده) |
| `OtpGeneratedEvent` | OTP ساخته و هش شد |
| `OtpVerifiedEvent` | OTP با موفقیت تأیید شد |
| `OtpVerificationFailedEvent` | تلاش ناموفق همراه با تعداد تلاش و باقی‌مانده |
| `OtpExpiredEvent` | OTP منقضی شد |
| `UserLoggedInEvent` | ورود موفق کاربر ثبت شد |
| `UserLoginFailedEvent` | ورود ناموفق همراه با شمارنده |
| `UserLockedOutEvent` | قفل‌شدن حساب پس از خطاهای پی‌درپی |

## Application (`Application/Auth`)

۹ Command در `Features/Commands`:

| گروه | Commandها |
|---|---|
| ورود با OTP | `SendOtp`, `VerifyOtp` |
| ورود با گوگل | `GoogleLogin` |
| توکن | `RefreshToken` |
| خروج و ابطال سشن | `Logout`, `LogoutAll`, `LogoutOthers`, `RevokeSession`, `AdminRevokeSession` |

دو Query در `Features/Queries`: `GetCurrentSession`, `GetUserSessions`.
سه Validator فقط برای `RefreshToken`, `SendOtp`, `VerifyOtp` وجود دارد.

### جریان‌های اصلی (از کد Handlerها)

**`SendOtp`** — شماره با `PhoneNumber.Create` نرمال می‌شود؛ اگر کاربری با آن شماره نباشد با
`User.RegisterByPhone` ساخته می‌شود (ایمیل موقت و بدون رمز) و اگر شماره در `InitialAdmin:PhoneNumbers`
باشد `PromoteToAdmin` می‌گیرد. سپس `IOtpService.ValidateRateLimitAsync` تعداد OTPهای ساخته‌شده در
پنجره را می‌سنجد (پیش‌فرض `MaxOtpPerWindow = 3` در `OtpRateLimitWindowMinutes = 10`)؛ در ادامه کد
۶ رقمی تولید، هش SHA256 ذخیره و رویداد `OtpGeneratedEvent` صادر می‌شود؛ ارسال پیامک از طریق
`ISmsService.SendOtpSMSAsync` (پیاده‌سازی Kavenegar) و سپس `SaveChangesAsync` انجام می‌شود.
اعتبار OTP در این Handler **۲ دقیقه** هاردکد شده است.

**`VerifyOtp`** — آخرین OTP فعال کاربر (`GetLatestActiveByUserIdAsync`) بارگذاری و با
`otp.Verify(code, now)` بررسی می‌شود؛ تلاش ناموفق `VerificationAttempts` را افزایش می‌دهد و
رویداد `OtpVerificationFailedEvent` را صادر می‌کند. در موفقیت، `ISessionService.CreateSessionAsync`
یک Refresh Token تصادفی می‌سازد، سشن فعال قبلی **همان دستگاه** را ابطال می‌کند و سشن جدید با انقضای
`AuthOptions.SessionExpirationDays` (پیش‌فرض ۳۰ روز) می‌سازد؛ خروجی `AuthResult` شامل
Access Token و Refresh Token است.

**`GoogleLogin`** — هویت گوگل پیش از Command در `Presentation/Auth/Services/HttpGoogleAuthenticationService`
با `HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme)` و claimهای
Email/GivenName/Surname/NameIdentifier استخراج می‌شود. Handler کاربر را با ایمیل می‌جوید؛ در نبود
کاربر `User.CreateExternal` (بدون رمز، ایمیل تأییدشده) می‌سازد، سشن ایجاد و Access Token صادر می‌کند.
طرح گوگل فقط وقتی `Authentication:Google:ClientId/Secret` تنظیم شده باشد به Pipeline اضافه می‌شود.

**`RefreshToken` (چرخش توکن)** — سشن با Refresh Token یافته می‌شود؛ سشن غیرفعال یا توکن نامطابق
پاسخ Unauthorized می‌گیرد. سپس سشن قبلی با دلیل `UserRequested` ابطال و یک Refresh Token تازه با
انقضای ۳۰ روزه صادر می‌شود؛ کاربر غیرفعال (`!user.IsActive`) رد می‌شود و JWT جدید برای سشن جدید صادر می‌گردد.

**خروج و ابطال** — `Logout` فقط سشن خودِ کاربر را ابطال می‌کند و در نبود توکن/سشن بی‌صدا موفق می‌شود.
`LogoutAll` با `RevokeAllByUserIdAsync` (دلیل `AllSessionsRevoked`) و `LogoutOthers` با
`RevokeAllExceptAsync` سشن جاری عمل می‌کنند. `RevokeSession` اگر سشن متعلق به کاربر نباشد Forbidden
می‌دهد و `AdminRevokeSession` با دلیل `AdminRevoked` کار می‌کند.

### Event Handlerها (`Application/Auth/EventHandlers` — ۷ کلاس)

| Handler | رویداد | اثر |
|---|---|---|
| `UserCreatedEventHandler` | `UserRegisteredEvent` | ساخت کیف پول کاربر با ارز `IRR` و ثبت audit |
| `UserLoggedInEventHandler` | `UserLoggedInEvent` | لاگ و audit «User login» |
| `UserLockedOutEventHandler` | `UserLockedOutEvent` | لاگ هشدار و audit قفل‌شدن |
| `UserPasswordChangedEventHandler` | `UserPasswordChangedEvent` | ابطال همه سشن‌ها با دلیل `PasswordChanged` + رویداد امنیتی |
| `UserPhoneChangedEventHandler` | `UserPhoneChangedEvent` | ابطال کش کاربر + audit |
| `UserDeactivatedEventHandler` | `UserDeactivatedEvent` | ابطال کش کاربر + audit |
| `AllSessionsRevokedEventHandler` | `AllSessionsRevokedEvent` | لاگ و رویداد سیستمی audit |

> توجه: نام فایل `UserPasswordChangedEventHandler.cs.cs` در مخزن دو پسوند `.cs` دارد (اشکال نام‌گذاری، نه منطق).

## API

همه مسیرها با قالب نسخه‌دار `api/v{version:apiVersion}` ساخته می‌شوند؛ قراردادهای مسیردهی، پاکت پاسخ و
کدهای خطا در [05-api/conventions.md](../../05-api/conventions.md) است و اینجا فقط تفاوت‌های همان ماژول می‌آید.

`AuthController` — `[Route("api/v{version:apiVersion}/auth")]` — بدون `[Authorize]` در سطح کلاس؛ ۷ اکشن:

| متد | مسیر | اکشن | دسترسی |
|---|---|---|---|
| GET | `google` | `GoogleLogin` | `[AllowAnonymous]` |
| GET | `google/callback` | `GoogleCallback` | `[AllowAnonymous]` |
| POST | `otp` | `RequestOtp` | `[AllowAnonymous]` + `[OtpRateLimit]` |
| POST | `otp/verify` | `VerifyOtp` | `[AllowAnonymous]` + `[OtpRateLimit]` |
| POST | `token/refresh` | `RefreshToken` | `[AllowAnonymous]` |
| DELETE | `session` | `Logout` | `[Authorize]` |
| DELETE | `sessions` | `LogoutAll` | `[Authorize]` |

`SessionController` — `[Route("api/v{version:apiVersion}/sessions")]` — `[Authorize]`؛ ۵ اکشن:
`GET` → `GetActiveSessions`، `GET current` → `GetCurrentSession`، `DELETE {sessionId:guid}` → `RevokeSession`،
`DELETE others` → `LogoutOtherSessions`، `DELETE` → `LogoutAllSessions`.

`AdminSessionController` — `[Route("api/v{version:apiVersion}/admin/users/{userId:guid}/sessions")]` — `[Authorize(Roles = "Admin")]`؛
۳ اکشن: `GET` → `GetUserSessions`، `DELETE {sessionId:guid}` → `RevokeUserSession`، `DELETE` → `RevokeAllUserSessions`.

Endpoint جانبی مرتبط: `GET api/v{version:apiVersion}/config/auth` در `Presentation/Common/Endpoints/ConfigController.cs`
(ناشناس) مقادیر عمومی `OtpLength`، `OtpResendSeconds`، `OtpExpirationMinutes` و `SessionExpirationDays` را برمی‌گرداند.

جمع این ماژول: **۳ کنترلر و ۱۵ اکشن HTTP**.

## زیرساخت

- `JwtTokenGenerator` (`Infrastructure/Auth/Services/`) توکن HS256 با claimهای `sub`, `jti`, `sid`, `nameid`,
  `ClaimTypes.MobilePhone`, و نقش `AppRoles.User` (و `AppRoles.Admin` در صورت ادمین‌بودن) صادر می‌کند؛
  عمر پیش‌فرض Access Token شصت دقیقه است (`JwtOptions`، بخش `Jwt`). اعتبارسنجی Bearer در
  `Presentation/Common/Extensions/AuthenticationExtensions.cs` فقط الگوریتم `HmacSha256` را می‌پذیرد و
  `ClockSkew` سی ثانیه است.
- `OtpService` هش SHA256، ارسال پیامک و سنجش سقف نرخ را انجام می‌دهد؛ `AuthService` وظیفه چرخش توکن و
  بررسی فعال‌بودن کاربر را دارد و `SessionService` ساخت/Refresh/ابطال سشن را مدیریت می‌کند.
- هش رمز عبور در `Infrastructure/Security/Services/PasswordHasher.cs` با BCrypt و `WorkFactor = 12` انجام می‌شود.
- سقف نرخ OTP دو لایه دارد: فیلتر HTTP `OtpRateLimitFilter` (کلید بر پایه IP، ۵ تلاش در ۱۰ دقیقه، پاسخ
  ۴۲۹ با هدر `Retry-After`) و سرویس `RateLimitService` روی Redis (پنجره لغزان ZSET با Lua) که با
  `ResilientRateLimitService` و کلیدشکنی Polly به `InMemoryRateLimitService` برمی‌گردد.
- `SessionActivityMiddleware` پس از هر درخواست `LastActivityAt` را با throttle پنج‌دقیقه‌ای به‌روز می‌کند و
  `ExpiredSessionCleanupJob` (ساعتی، قفل توزیع‌شده `jobs:expired-session-cleanup`) سشن‌های منقضی را
  `MarkExpired` می‌کند.
- پیکربندی: `JwtOptions` (بخش `Jwt`؛ حداقل ۳۲ کاراکتر برای `Key`)، `AuthOptions` (بخش `Auth`)،
  `OtpOptions` (بخش `Otp`؛ `Length=6`, `ExpirationMinutes=2`, `MaxAttempts=3`) و
  `GoogleAuthSettings` (بخش `Authentication:Google`).
- مدل EF سشن یک ایندکس یگانه فیلترشده `IX_UserSessions_UserId_DeviceInfo_Active` روی
  `"IsRevoked" = false` دارد که سیاست «یک سشن فعال به ازای هر دستگاه» را در سطح دیتابیس تضمین می‌کند.

## نکات و محدودیت‌ها

- **دو منبع متفاوت برای انقضای OTP**: `AuthOptions.OtpExpirationMinutes` مقدار ۵ دارد اما
  `SendOtpHandler` اعتبار ۲ دقیقه را هاردکد کرده است؛ `OtpOptions.ExpirationMinutes` (مقدار ۲) تنها
  به Endpoint عمومی `config/auth` سرویس می‌دهد. یعنی تنظیمات و رفتار واقعی یکسان نیستند.
- سیاست سشن «یک سشن فعال برای هر دستگاه» است، نه سقف تعداد سشن؛ محدودیت تعددی در کد وجود ندارد.
- تشخیص استفاده مجدد از Refresh Token (reuse detection) و ثبت تاریخچه توکن‌های مصرف‌شده وجود ندارد؛
  فقط سشن با توکن تطبیق داده می‌شود و توکن جدید جایگزین می‌شود.
- `UserLoggedInEvent`/`UserLoginFailedEvent`/`UserLockedOutEvent` در `Domain/Security` تعریف شده‌اند،
  اما در Handlerهای این ماژول صادر نمی‌شوند؛ مسیر واقعی ورود با OTP یا گوگل است و این رویدادها به
  منطق رمز عبور در `Domain/User` تعلق دارند.
- ورود گوگل نیازمند تنظیم `Authentication:Google` است؛ در نبود آن طرح گوگل ثبت نمی‌شود و
  Endpointهای `google` و `google/callback` قابل استفاده نخواهند بود.
- هدر `X-Guest-Token` در `CurrentUserService` خوانده می‌شود و مربوط به سبد مهمان است، نه این ماژول
  (به [cart.md](../cart.md) مراجعه کنید).

## اسناد مرتبط

- کاربران، آدرس‌ها و چرخه قفل حساب: [users.md](users.md)
- قراردادهای مسیر، پاکت پاسخ و Rate Limiting سراسری: [05-api/conventions.md](../../05-api/conventions.md)
- DataProtection، ابزارهای اسکن و سخت‌سازی عملیاتی: [07-operations/security.md](../../07-operations/security.md)
- Outbox، قفل توزیع‌شده، کش و Jobها: [02-architecture/cross-cutting.md](../../02-architecture/cross-cutting.md)
- ارسال پیامک و Kavenegar: [notifications.md](../notifications.md)
