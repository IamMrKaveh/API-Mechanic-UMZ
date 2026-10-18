# گزارش ممیزی امنیتی پروژه Mechanic.Api (.NET 10)

**تاریخ:** ۲۰۲۶-۱۰-۱۸
**دامنه:** بررسی کامل Solution (Application, Domain, Infrastructure, Presentation, SharedKernel, SharedContracts) + پیکربندی (appsettings, Docker, CI) با تمرکز بر OWASP Top 10.
**روش:** بررسی دستی کد مسیرهای حساس + اسکن خودکار الگوهای ناامن (SQL خام، رازهای هاردکد، رمزنگاری ضعیف، deserialization، SSRF، کنترل دسترسی).
**نتیجه کلی:** ۲۶ یافته (۳ بحرانی، ۸ مهم، ۹ متوسط، ۶ کم/اطلاعاتی). چندین سازه امنیتی خوب در پروژه موجود است (فاز ۶) که باید حفظ شود.

---

## فهرس اولویت‌ها

| فاز | موضوع | تعداد یافته | شدت |
|---|---|---|---|
| ۰ | رازهای افشاشده در مخزن (فوری — قبل از هر تغییر دیگری) | ۲ | بحرانی |
| ۱ | کنترل دسترسی معیوب (Broken Access Control) | ۴ | بحرانی/مهم |
| ۲ | امنیت پرداخت، توکن و نشست | ۵ | مهم |
| ۳ | سخت‌سازی وب و آپلود فایل | ۶ | متوسط |
| ۴ | نظافت پیکربندی و زیرساخت | ۵ | کم |
| ۵ | نقشه راه اجرایی و تأیید نهایی | — | — |
| ۶ | نقاط قوت امنیتی موجود (حفظ شود) | — | — |

---

## فاز ۰ — رازهای افشاشده در مخزن (اولویت مطلق، فوری)

### SEC-01 [بحرانی] اعتبارنامه‌هایproduction در فایل‌های ردیابی‌شده Git
**محل‌ها:**
- `Presentation/appsettings.json:4,9` — کلید Access/Secret آرشیو S3 (ArvanCloud)
- `Presentation/appsettings.json:119` — کلید امضای JWT: `"Breaking355662Bad!SecureKeyForHS256Algo1234"`
- `Presentation/appsettings.json:123` — کلید API پیامک Kavenegar
- `Presentation/appsettings.json:222,230` — تکرار کلیدهای S3 در بخش `Storage`
- **`otp.json` (رأس مخزن، ۳ مگابایت):** لاگ یک نشستِ عامل هوشمند که نسخه کامل `appsettings.Production.json` را در خود دارد؛ شامل **اعتبارنامه زندهٔ دیتابیس production روی Supabase** (`postgres.cjcahidscpiatcgctfnx` روی `aws-1-ap-northeast-1.pooler.supabase.com`، خطوط ~۱۱۱۶۵–۱۲۱۷۱)، کلید JWT و کلید Kavenegar.
- `appsettings.Production.json` در تاریخچهٔ Git موجود است (از working tree حذف شده اما در تاریخچه مانده).

**سناریوی حمله:** هر کسی با دسترسی به مخزن (یا تاریخچهٔ آن) می‌تواند توکن JWT جعلی با نقش Admin بسازد (`alg=HS256` همان کلید)، به دیتابیس production وصل شود و داده‌ها را بخواند/بنویسد، از S3 فایل حذف کند یا پیامک دلخواه بفرستد.

**راه‌حل تمیز (ترتیب اجرا):**
1. **چرخش (rotate) همهٔ اعتبارنامه‌ها — فرض کنید لو رفته‌اند:**
   - رمز دیتابیس Supabase (پنل Supabase → Database → Reset password)
   - مالکیت/merchant جدید برای S3 ArvanCloud
   - کلید API جدید Kavenegar
   - تولید کلید JWT جدید با `RandomNumberGenerator` (حداقل ۴۳ بایت → base64) و **ذخیره در متغیر محیطی/Secret Manager** — نه در کد.
2. **پاک‌سازی تاریخچهٔ Git:** فایل‌های `otp.json` و `appsettings.Production.json` (نسخهٔ حاوی رازها) را با `git filter-repo --invert-paths --path otp.json --path Presentation/appsettings.Production.json` از تاریخچه حذف کنید و force-push کنید (هماهنگ با تیم). پس از پاک‌سازی، رازهای قبلی بی‌اعتبار خواهند شد زیرا چرخش خورده‌اند.
3. **جابه‌جایی رازها به محیط:** در کد، تمام مقادیر حساس از `IOptions<T>` با اعتبارسنجی هنگام استارت خوانده شوند (الگوی موجود `ValidateRequiredConfiguration` را گسترش دهید) و در appsettings فقط **placeholder** بماند:
   ```json
   "Jwt": { "Key": "" }   // مقدار واقعی از env: Jwt__Key
   ```
4. **جلوگیری از تکرار:** فایل `.gitleaks.toml` موجود است اما `otp.json`/appsettings را پوشش نمی‌دهد. آن را طوری تنظیم کنید که در pre-commit hook و CI اجرا شود و در صورت کشف راز، بیلد fail شود:
   ```bash
   gitleaks protect --staged   # pre-commit
   gitleaks detect --no-git    # CI
   ```

**تأیید:** `git log --all -p | grep -c "Breaking"` باید صفر باشد؛ `dotnet run` فقط با env varها بالا بیاید.

---

## فاز ۱ — کنترل دسترسی معیوب (Broken Access Control)

### SEC-02 [بحرانی] کاربران عادی می‌توانند برداشت دیگران را علامت بزنند/رد کنند
**محل:** `Presentation/Wallet/Endpoints/WalletController.cs:177-187` (`POST /{id}/reject`) و `:189-202` (`POST /{id}/mark-paid`) — کنترلر فقط `[Authorize]` کلاسی دارد (بدون نقش).
هندلرها هیچ بررسی نقش/مالکیت نمی‌کنند: `MarkWithdrawalPaidHandler.cs:25` فقط فراخوان را به‌عنوان `adminId` ثبت می‌کند و در خط ۵۳ کیف پول کاربرِ دیگری را دبیت می‌کند؛ `RejectWithdrawalHandler.cs:23-35` رزرو را آزاد می‌کند.

**سناریو:** کاربر با توکن خودش `POST /api/v1/wallet/{withdrawalId}/mark-paid` را صدا می‌زند → برداشتِ کاربر دیگری «پرداخت‌شده» علامت می‌خورد (کیف پول قربانی دبیت می‌شود در حالی که پولی به او نرسیده) یا رد می‌شود (موجودی رزروشده آزاد می‌شود). مسیر درست و محافظت‌شده در `AdminWalletWithdrawalController.cs:74-89` با `[Authorize(Roles = "Admin")]` وجود دارد.

**راه‌حل تمیز (دفاع لایه‌ای، هم.repository):**
1. در کنترلر، همان الگوی مسیر ادمین را تکرار کنید:
   ```csharp
   [HttpPost("{id:guid}/reject")]
   [Authorize(Roles = "Admin")]      // افزودن
   [HttpPost("{id:guid}/mark-paid")]
   [Authorize(Roles = "Admin")]      // افزودن
   ```
2. دفاع در عمق در هندلر (الگوی موجود پروژه از `CancelOrderHandler.cs:22` را کپی کنید — بررسی `currentUserService.IsAdmin`):
   ```csharp
   if (!currentUserService.IsAdmin)
       return ServiceResult<Unit>.Failure("دسترسی مجاز نیست.");
   ```
3. تست‌های موجود برای مسیر ادمین (`AdminWalletWithdrawalController`) را حفظ کنید؛ تست جدید «کاربر غیرادمین 403 می‌گیرد» اضافه کنید (بدون تضعیف تست‌های موجود).

### SEC-03 [مهم] IDOR روی پروفایل کاربر: خواندن/تغییر هر کاربر با شناسه
**محل:** `Presentation/User/Endpoints/UserController.cs:13-32` — `GET/PUT /api/v1/users/{id}` با `[Authorize]` کلاسی اما بدون بررسی مالکیت؛ `GetUserByIdHandler`/`UpdateUserHandler` هیچ `ICurrentUserService` ندارند و `UserProfileDto` شامل تلفن/ایمیل/آدرس است.

**راه‌حل تمیز (هم‌راستا با الگوی موجود `GetPaymentsByOrderHandler.cs:27`):**
```csharp
// GetUserByIdHandler / UpdateUserHandler
var currentUserId = currentUserService.UserId!.Value;
if (request.UserId != currentUserId && !currentUserService.IsAdmin)
    return ServiceResult<...>.Failure("دسترسی مجاز نیست."); // یا Forbid
```
اگر کاربر «پروفایل خودم» را می‌خواهد، مسیر `/api/v1/users/me` (خواندن/ویرایش از claims) اضافه شود؛ مسیر `{id}` برای ادمین‌ها با بررسی نقش محدود بماند.
**تأیید:** تست یکپارچه/واحدی: توکن کاربر A روی شناسهٔ کاربر B → 403.

### SEC-04 [مهم] اندپوینت افشای اطلاعات تخفیف بدون احراز هویت
**محل:** `Presentation/Discount/Endpoints/AdminDiscountsController.cs:55-56` — `[AllowAnonymous] GetDiscountInfo` داخل کنترلر `Roles="Admin"`.

**تحلیل:** اگر این مسیر برای «بررسی کد تخفیف در سبد خرید» لازم است، باید **کنترلر عمومی مجزایی** با DTO حداقلی (بدون آمار مصرف/سقف) بگیرد، نه `[AllowAnonymous]` زیر مسیر `/admin/`.
**راه‌حل تمیز:** یا (الف) حذف `[AllowAnonymous]` و فقط ادمین‌ها، یا (ب) انتقال به `DiscountsController` عمومی با پاسخ حداقلی.

### SEC-05 [مهم] لیست سفید IP ادمین عملاً غیرفعال است
**محل:** `Presentation/Common/Middleware/SecurityMiddleware.cs:16` — پیشوند `/api/admin` بررسی می‌شود، اما همهٔ مسیرهای ادمین `api/v{version}/admin/...` هستند → هیچ‌گاه مطابقت نمی‌کند. همچنین `:34-35` وقتی لیست خالی باشد **fail-open** (اجازه به همه) است.

**راه‌حل تمیز:**
```csharp
private static bool IsAdminPath(PathString path) =>
    path.StartsWithSegments("/api") &&
    path.Value!.Contains("/admin/", StringComparison.OrdinalIgnoreCase);
```
و fail-closed: اگر تنظیمات ادمین وجود دارد ولی لیست خالی است → رد (۴۰۳) + هشدار در لاگ. لیست از `Security:AdminIpWhitelist` در production باید آی‌پی‌های واقعی اتاق عملیات را داشته باشد.
**تأیید:** از آی‌پی خارج از لیست، `GET /api/v1/admin/users` → 403.

---
## فاز ۲ — امنیت پرداخت، توکن و نشست (مهم)

### SEC-06 [مهم] وب‌هوک پرداخت بدون هیچ احراز هویت (پیش‌فرض خاموش)
**محل‌ها:** `Presentation/Payment/Endpoints/PaymentsController.cs:65` — `POST /api/v{version}/payments/webhooks/{gateway}` هیچ `[Authorize]`/امضا/IP ندارد؛ `appsettings.json:94-95` هر دو فلگ `Payment.Callback.SignatureRequired=false` و `Payment.Callback.IpWhitelistRequired=false` هستند؛ `WebhookIpWhitelistMiddleware.cs:41-49` وقتی فلگ خاموش است **fail-open** لاگ می‌زند و رد می‌کند؛ nonce validation هم در `PaymentService.cs:189-209` پشت فلگ است؛ `Zarinpal:AllowedIps: []` خالی است.

**تحلیل تأثیر:** وب‌هوک جعلی می‌تواند وضعیت تراکنش‌ها را دستکاری کند (حذف/تأخیر تأیید سفارش = DoS تجاری). خودِ واریز به کیف پول نیازمند verify واقعی از درگاه است (`PaymentService.cs:145` مبلغ با رکورد سمت سرور تطبیق داده می‌شود — این قسمت سالم است)، بنابراین «شارژ جعلی» ممکن نیست، اما خرابکاری در وضعیت سفارش/تراکنش ممکن است.

**راه‌حل تمیز:**
1. در production هر دو فلگ را `true` کنید (`appsettings.Production.json`).
2. `Zarinpal:AllowedIps` را با آی‌پی‌های رسمی زرین‌پال پر کنید.
3. fail-closed: اگر فلگ روشن است ولی لیست خالی → رد با ۵۰۳ + هشدار (نه عبور بی‌قید).
4. اعتبارسنجی امضا: nonce را همیشه اجباری کنید (حذف قید فلگ‌بودن در مسیر production یا پیش‌فرض true).
5. `{gateway}` route param نادیده گرفته می‌شود — یا به allowlist تبدیل شود یا از مسیر حذف شود.

### SEC-07 [مهم] ری‌فرش‌توکن به‌صورت متن ساده ذخیره و مقایسه می‌شود
**محل‌ها:** `Infrastructure/Auth/Configurations/SessionConfiguration.cs:21-25` (ذخیره متن ساده)، `Infrastructure/Auth/Repositories/SessionRepository.cs:16` (`s.RefreshToken == refreshToken`).

**راه‌حل تمیز:** هنگام ساخت نشست، هش SHA-256 توکن را ذخیره کنید و جست‌وجو را با ورودی هش‌شده انجام دهید:
```csharp
// ساخت: token = RefreshToken.Generate(); storeHash = SHA256(token.Value)
// جست‌وجو: var hash = SHA256(requestToken); s.RefreshTokenHash == hash
```
نقاط قوت موجود را حفظ کنید: چرخش توکن (`SessionService.cs:73-90`)، کوکی `__Host-refresh` با HttpOnly/Secure/SameSite=Strict، ابطال در logout و تغییر رمز (`UserPasswordChangedEventHandler.cs:32-35`).

### SEC-08 [مهم] کد OTP با SHA-256 ساده (بدون نمک) ذخیره می‌شود
**محل:** `Infrastructure/Auth/Services/OtpService.cs:21`.

**تحلیل:** کد ۶ رقمی فقط ۱۰⁶ حالت دارد؛ با افشای دیتابیس، حملهٔ آفلاین در چند ثانیه موفق می‌شود. سدهای آنلاین موجود هستند (سقف ۵ تلاش، محدودیت ارسال، CSPRNG).

**راه‌حل تمیز:** هش با کلید سرور (pepper) یا BCrypt با work factor پایین:
```csharp
using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_pepper)); // pepper از env
var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(code)));
```
(کل Pepper از SEC-01 چرخش/محیط‌گیری پیروی کند.)

### SEC-09 [مهم] قفل حساب پس از تلاش‌های ناموفق پیاده‌سازی نشده
**محل‌ها:** `Application/Auth/Features/Shared/AuthOptions.cs:13-14` تنظیمات `MaxFailedLoginAttempts=5`/`LockoutDurationMinutes=15` تعریف شده اما **هرگز به کانفیگ bind و هرگز خوانده نمی‌شود**؛ `VerifyOtpHandler.cs` و `GoogleLoginHandler.cs` هیچ `user.IsLockedOut` بررسی نمی‌کنند. منطق دامنه در `Domain/User/Aggregates/User.cs:220-229` وجود دارد ولی فراخوانی نمی‌شود.

**راه‌حل تمیز:**
```csharp
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
// در VerifyOtpHandler (قبل از تولید/بررسی کد):
var lockResult = user.RegisterFailedAttempt(now, options.MaxFailedLoginAttempts, options.LockoutDurationMinutes);
if (lockResult.IsFailure) return lockResult.Error;
```
(نام متد دامنه را از `User.cs:220-229` بخوانید و استفاده کنید.)

### SEC-10 [مهم] پنجرهٔ ابطال توکن دسترسی تا ۶۰ دقیقه
**توضیح:** `AuthenticationExtensions.cs:54-65` در `OnTokenValidated` فقط الگوریتم را بررسی می‌کند؛ توکن‌های نشست‌های ابطال‌شده (logout همه، تغییر رمز، ابطال ادمین) تا انقضا معتبر می‌مانند. `SessionActivityMiddleware.cs:19,49` فقط فعالیت را ثبت می‌کند و درخواست را رد نمی‌کند.

**راه‌حل تمیز (دو گزینه، ترکیبی بهترین):**
- کوتاه‌مدت: کاهش عمر access token به ۱۵ دقیقه (refresh چرخش دارد).
- بلندمدت: بررسی ابطال در `OnTokenValidated` از طریق claim `sid` با کش کوتاه (مثلاً ۳۰ ثانیه) روی `SessionRepository`:
```csharp
if (context.Principal!.FindFirst("sid")?.Value is { } sid &&
    !await sessionStore.IsActiveCachedAsync(sid, ct)) context.Fail("Session revoked");
```

---

## فاز ۳ — سخت‌سازی وب و آپلود فایل (متوسط)

### SEC-11 [متوسط] Open Redirect در کالبک شارژ کیف پول
**محل:** `Presentation/Wallet/Endpoints/WalletController.cs:286-296` — `ResolveFrontendBaseUrl()` هدر `Origin` خام را بدون allow-list می‌پذیرد و در خط ۱۱۴ کاربر را به `frontendBase + callbackPath` ریدایرکت می‌کند (کنترلر `topup/callback` در خط ۷۲ `[AllowAnonymous]` است).

**راه‌حل تمیز (هم‌راستا با الگوی موجود `CurrentUserService.cs:59-76` که Origin را در برابر allow-list بررسی می‌کند):**
```csharp
var origin = Request.Headers["Origin"].ToString();
if (_allowedOrigins.Contains(origin)) return origin;      // از Security:AllowedOrigins
return _frontendOptions.BaseUrl;                          // پیش‌فرض امن
```

### SEC-12 [متوسط] CORS با `AllowAnyHeader`/`AllowAnyMethod` همراه اعتبارنامه
**محل:** `Presentation/Common/Extensions/CorsExtensions.cs:24-25`.
**راه‌حل تمیز:** مethods و هدرهای موردنیاز را صریح کنید: `.WithMethods("GET","POST","PUT","DELETE").WithHeaders("Content-Type","Authorization","X-Requested-With")` (اصل least privilege). Origins از کانفیگ — این بخش درست است.

### SEC-13 [متوسط] نام فایل آپلود از کلاینت گرفته می‌شود
**محل‌ها:** `Infrastructure/Storage/Services/S3FileStorageService.cs:28-29` (قطعهٔ آخر کلید = نام خام کلاینت؛ `CreateBrandHandler.cs:55-57` پسوند از `Path.GetExtension(clientFileName)`؛ `UpdateBrandHandler.cs:40-42` مشابه).
**تحلیل:** `FilePath.CreateForUpload` از traversal (`..`، `:`) جلوگیری می‌کند (خوب است)، ولی پسوند دوسرخته (`x.php.jpg`) و انکدینگ/URL عمومی باقی می‌ماند.
**راه‌حل تمیز:**
```csharp
var ext = Path.GetExtension(fileName).ToLowerInvariant();
// فقط پسوندهای مجاز تصویب‌شدهٔ content-type معتبر:
if (!AllowedExtensions.Contains(ext)) ext = ".bin";
var key = $"{folder}/{Guid.NewGuid()}{ext}";   // نام کاملاً سمت سرور
```
برای Brand همان الگو: پسوند از content-type اعتبارسنجی‌شدهٔ magic-byte مشتق شود (`ImageSniffer` موجود)، نه از `Path.GetExtension`.

### SEC-14 [متوسط] هندلرهای کیف پول به `userId` درخواست اعتماد می‌کنند
**محل‌ها:** `GetWalletLedgerHandler.cs:15-17` (`request.UserId` جای کاربر جاری)، `ExportWalletLedgerHandler.cs:17` (بدون `ICurrentUserService`)، همچنین `GetWalletTransfersHandler.cs:14`، `GetFraudAlertsHandler.cs:16`، `GetPendingDebitRequestsByUserHandler.cs:18`، `GetWalletBalanceHandler.cs:19` (fallback به request).
**تحلیل:** امروز فقط از کنترلرهای `[Authorize(Roles="Admin")]` قابل دسترسی است، اما شکننده است (یک اشتباه در کنترلر = افشای کلی).
**راه‌حل تمیز:** در همهٔ این هندلرها `request.UserId` حذف و از `currentUserService` استفاده شود؛ برای ادمین، پارامتر اختیاری با نقش‌آگاهی (اگر `IsAdmin`).

### SEC-15 [متوسط] Rate limit وقتی آی‌پی خالی است کاملاً رد می‌شود
**محل:** `RateLimitMiddleware.cs:22-26` — وقتی `RemoteIpAddress` تهی است، فیلتر رد می‌شود (بدون محدودیت). کلیدها global (نه به‌ازای endpoint).
**راه‌حل تمیز:** آی‌پی ناشناس = باکت مجزای محدودشده (`"unknown"` با حد پایین)؛ افزودن policy به‌ازای endpoint برای `/auth/otp`.

### SEC-16 [متوسط] اندپوینت «تعداد خوانده‌نشده» بدون احراز هویت
**محل:** `Presentation/Notification/Endpoints/NotificationsController.cs:34` — `[AllowAnonymous]` روی `unread-count` در حالی که بقیهٔ کنترلر `[Authorize]` است.
**راه‌حل تمیز:** حذف `[AllowAnonymous]` (بازمهندسی برای «تعداد ۰» از مهمان اگر لازم است — یا با سرویس بدون افشای داده).

---
## فاز ۴ — نظافت پیکربندی و زیرساخت (کم)

### SEC-17 [کم] `appsettings.Development.json` هم شامل راز است
`Presentation/appsettings.Development.json:9-12` رمز دیتابیس محلی و `:31` همان کلید JWT هاردکد شده. توسعه‌دهنده‌ها باید `dotnet user-secrets` استفاده کنند. **راه‌حل:** حذف مقادیر از فایل ردیابی‌شده + آموزش تیم؛ در dev کلید جداگانه با fallback generate.

### SEC-18 [کم] `docker-compose.yml` چند اشکال امنیتی/عملیاتی
`docker-compose.yml`: `TrustServerCertificate=True;Encrypt=False` در connection string خط ۵۱ (TLS/Egress دیتابیس خاموش)؛ `ASPNETCORE_ENVIRONMENT=Development` برای سرویس api خط ۵۰ (صفحه‌های خطای پرجزئیات و رفتار dev در محیط کانتینری)؛ `dockerfile: MainApi/Dockerfile` خط ۴۱ مسیر شکسته (Dockerfile واقعی `Presentation/Dockerfile`)؛ رمزار عبور روی خط فرمان `sqlcmd` خط ۲۹/۱۶ در process list دیده می‌شود.
**راه‌حل تمیز:** حذف `TrustServerCertificate` (استفاده از cert معتبر یا DDNS داخلی)، `ASPNETCORE_ENVIRONMENT=Production` + override از `.env`، اصلاح مسیر Dockerfile، استفاده از فایل secret/`MSSQL_SA_PASSWORD` فقط env.

### SEC-19 [کم] اعتبارسنجی TLS در رشته‌های اتصال production خاموش است
در نسخهٔ تاریخی `appsettings.Production.json` (داخل `otp.json`): `Trust Server Certificate=true` در هر ۴ رشتهٔ اتصال Supabase. حتی با TLS، اعتبارسنجی گواهی غیرفعال است (حمله MITM در شبکه). **راه‌حل:** حذف این گزینه (Supabase گواهی معتبر دارد) — همراه چرخش SEC-01.

### SEC-20 [کم] `AllowedHosts: "*"`
`appsettings.json:12` — Host header injection (mangling لینک‌ها/کش). **راه‌حل:** لیست دامنه‌های واقعی (`ledka-co.ir`, دامنهٔ API).

### SEC-21 [کم] عمر OTP هاردکد ۲ دقیقه虽然有 config تنظیم‌شده
`SendOtpHandler.cs:42` از `TimeSpan.FromMinutes(2)` استفاده می‌کند در حالی که `Auth:OtpExpirationMinutes` (پیش‌فرض ۵) نادیده گرفته می‌شود. **راه‌حل:** خواندن از IOptions — رفتار امنیتی منسجم با پیکربندی.

### SEC-22 [کم/اطلاعاتی] کدهای مرده با اعتماد به `IsAdmin` از درخواست
`Presentation/Support/Requests/SupportRequests.cs:12` (`CloseTicketRequest(bool IsAdmin)`) و `Application/Support/Features/Queries/GetTicket/GetTicketHandler.cs:20` (اعتماد به `RequestingUserId, IsAdmin` خودِ درخواست) — امروز استفاده نمی‌شوند. **راه‌حل:** حذف یا سیم‌کشی از claims (اگر قرار است استفاده شوند، پرچام `IsAdmin` هرگز نباید از ورودی کلاینت بیاید).

### SEC-23 [اطلاعاتی] `MockGatewayController` ورودی را escape‌نشده در HTML بازتاب می‌دهد
`Presentation/Payment/Endpoints/MockGatewayController.cs:22,24` — فقط در Development باز می‌شود (`:15-16`)، ولی برای شبیه‌سازی جعلی در staging خطرناک است. **راه‌حل:** تضمین `EnableMockGateway=false` در staging/production + HtmlEncode یا استفاده از view.

### SEC-24 [اطلاعاتی] ساخت خودکار کاربر برای هر شماره تلفن
`SendOtpHandler.cs:24-33` برای شمارهٔ ناشناس کاربر می‌سازد (طرح «اول OTP»). پاسخ یکسان است (خوب)، ولی ثبت‌نام معمولی وجود ندارد → هر دارندهٔ شمارهٔ معتبر حساب می‌سازد. این جنبهٔ طراحی محصول است، نه باگ؛ فقط اگر ثبت‌نام با تأیید دامنهٔ دیگری همر CONTEXT، مستند شود.

### SEC-25 [اطلاعاتی] ارتقای ادمین از طریق کانفیگ
`appsettings.json:110-114` — `InitialAdmin:PhoneNumbers` هنگام ورود نقش Admin می‌دهد. **توصیه:** این لیست فقط برای bootstrap اولیه باشد و در production خالی/از env با حساب مخصوص خوانده شود؛ تغییر در مخزن نمایان باشد.

---

## فاز ۵ — نقشه راه اجرایی و تأیید

**ترتیب اجرا (هر فاز مستقل قابل عرضح):**

1. **فوری (بدون تغییر کد، اول هر کار دیگر):** چرخش اعتبارنامه‌ها (SEC-01). تأمین env/secret manager. بدون این گام، هر اصلاح کد بی‌فایده است.
2. **فوری (پس از چرخش):** پاک‌سازی تاریخچهٔ Git (SEC-01#2) + gitleaks در pre-commit و fail در CI.
3. **روز ۱–۲ کد (بحرانی):** SEC-02، SEC-03، SEC-05 — تغییرات کوچک و متمرکز روی همان الگوهای موجود پروژه (`[Authorize(Roles)]` + بررسی `ICurrentUserService.IsAdmin` در هندلر). تست‌های واحد/یکپارچهٔ جدید برای «۴۰۳».
4. **هفتهٔ ۱:** SEC-06,07,08,09 (پرداخت + توکن/نشست).
5. **هفتهٔ ۲:** SEC-10..16 (نشست/ردیابی ابطال، وب، آپلود، rate limit).
6. **هفتهٔ ۳:** فاز ۴ (پیکربندی، docker، نظافت) + کدهای مرده.
7. **مستمر:** فاز ۶ + بازبینی امنیتی دوره‌ای.

**چک‌لیست تأیید نهایی:**
- `dotnet build -warnaserror` + اجرای کامل ۱۱٬۵۱۳ تست (بدون رگرسیون — تست‌های فعلی حفظ شوند).
- تست‌های امنیتی جدید: ۴۰۳ برای SEC-02/03/05؛ هش توکن (SEC-07)؛ fail-closed وبهوک (SEC-06).
- اسکن: `dotnet list package --vulnerable`؛ `gitleaks detect`؛ `npm audit` (اگر اگر前端 جدا است)؛ docker `trivy image` (در محیط دارای Docker).
- بررسی دستی: نیازمند تغییرات کانفیگ production (کلیدهای rotated، فلگ‌ها روشن، allow-listها پر) پیش از استقرار.

---

## فاز ۶ — نقاط قوت امنیتی موجود (حفظ شود)

این موارد در پروژه د دستکاری نمی‌شوند:
1. **هش رمز با BCrypt.Net work factor 12** (`Infrastructure/Security/Services/PasswordHasher.cs:12`).
2. **تولید OTP با CSPRNG** (`Domain/Security/ValueObjects/OtpCode.cs:31`) و ذخیرهٔ هش‌شده.
3. **کوکی ری‌فرش امن** (`__Host-refresh`، HttpOnly/Secure/SameSite=Strict — `AuthCookieService.cs:37-56`) + چرخش توکن + `[ValidateAntiForgeryToken]` روی refresh (`AuthController.cs:90`).
4. **هدرهای امنیتی کامل** — HSTS/CSP (با nonce)/X-Frame/Referrer/Permissions + COOP/COEP/CORP (`SecurityHeadersMiddleware.cs`).
5. **پارامتری‌سازی کامل SQL** — تمام Dapper/EF از پارامتر استفاده می‌کنند؛ هیچ تزریق SQL یا فرمان یا XXE یافت نشد.
6. **تأیید مبلغ پیش از اعمال پرداخت** (`PaymentService.cs:145`, `CompleteWalletTopUpHandler.cs:66-85`).
7. **JWT validation سختگیرانه** (issuer/audience/lifetime/alg pinned HS256/ClockSkew 30s — `AuthenticationExtensions.cs:33-65`).
8. **رمزنگاری کش Redis با AES-GCM** (`EncryptedRedisCacheService.cs:126,149`).
9. **بدون BinaryFormatter/MD5/SHA1/DES/new Random** در کد production؛ لاگ بدون بدنهٔ درخواست/هدر Authorization.
10. **RateLimitMiddleware با IP اتصال** (نه X-Forwarded-For قابل جعل — تا وقتی پروکسی مطمئن تنظیم نشده).

---

*پایان گزارش — این ممیزی تحلیلی است و هیچ تغییری در کد اعمال نشده. برای اعمال اصلاحات فازهای ۱–۴ (سازگار با معماری موجود، بدون commit بدون اجازه)، اعلام کنید.*
