# گزارش ممیزی امنیتی پروژه Mechanic.Api (.NET 10)

**تاریخ:** ۲۰۲۶-۱۰-۱۸
**دامنه:** بررسی کامل Solution (Application, Domain, Infrastructure, Presentation, SharedKernel, SharedContracts) + پیکربندی (appsettings, Docker, CI) با تمرکز بر OWASP Top 10.
**روش:** بررسی دستی کد مسیرهای حساس + اسکن خودکار الگوهای ناامن (SQL خام، رازهای هاردکد، رمزنگاری ضعیف، deserialization، SSRF، کنترل دسترسی).
**نتیجه کلی:** ۲۶ یافته (۳ بحرانی، ۸ مهم، ۹ متوسط، ۶ کم/اطلاعاتی). چندین سازه امنیتی خوب در پروژه موجود است (فاز ۶) که باید حفظ شود.

### ✅ وضعیت اجرا — فاز ۰ پیاده‌سازی شد (۲۰۲۶-۱۰-۱۸)
- **انتقال رازها به `.env`:** کلید JWT، کلید Kavenegar، کلیدهای S3/ArvanCloud، رشته‌های اتصال PostgreSQL و شماره تلفن ادمین از `appsettings.json` و `appsettings.Development.json` خارج و به فایل **gitignored** `.env` منتقل شدند (الگوی متغیرها با `__` مطابق جدایی سازندهٔ .NET: `Jwt__Key`، `Storage__SecretKey`، `ConnectionStrings__DefaultConnection`، …). قالب commit‌شدهٔ `.env.example` ساخته شد.
- **Fail-closed:** مقدار قبلی هر راز در appsettings جایگزین `""` + کامنت راهنما شد؛ در نبود `.env` اپ با خطای صریح `ValidateRequiredConfiguration` متوقف می‌شود (قبلاً هم این اعتبارسنجی وجود داشت و حالا عملاً کار می‌کند).
- **لودر `.env` بدون وابستگی NuGet:** `Presentation/Common/Configuration/DotEnvLoader.cs` + ۸ تست واحد؛ لودر از cwd به سمت ریشهٔ مخزن بالا می‌رود (app از `Presentation/` اجرا می‌شود) و متغیرهای محیطی واقعی همیشه بر `.env` مقدم هستند.
- **`otp.json`** (لاگ ۳MB یک نشست AI که نسخهٔ کامل `appsettings.Production.json` با اعتبارنامه‌های زندهٔ Supabase در خود داشت) از Git و دیسک حذف شد.
- **`docker-compose.yml`** با `env_file: .env` به زنجیرهٔ متغیرها وصل شد.
- **چرخش اعتبارنامه‌ها و بازنویسی تاریخچهٔ Git طبق تصمیم شما انجام نشد** (مخزن خصوصی؛ بدون نشت تأییدشده). اگر روزی مخزن عمومی یا تیم گسترش پیدا کرد: rotate + `git filter-repo` (حذف `otp.json` و `appsettings.Production.json` از تاریخچه) + gitleaks در pre-commit و CI.
- **تأیید:** Build ۰ خطا/۰ هشدار؛ ۱۱٬۵۲۱ تست پاس (۱۱٬۵۱۳ baseline + ۸ تست جدید)؛ اجرای واقعی اپ با `.env` تأیید شد. کامیت: `fix security progress`.

### ✅ وضعیت اجرا — فاز ۱ پیاده‌سازی شد (۲۰۲۶-۱۰-۱۸)
- **SEC-02 (بحرانی):** روی `POST /wallet/{id}/reject` و `POST /wallet/{id}/mark-paid` صفت `[Authorize(Roles = "Admin")]` اضافه شد + **دفاع در عمق** در هندلرها (`MarkWithdrawalPaidHandler`/`RejectWithdrawalHandler`) با بازگرداندن `Forbidden` اگر فراخوان ادمین نباشد. مسیر درست ادمین (`AdminWalletWithdrawalController`) دست‌نخورده ماند.
- **SEC-03 (مهم):** `GetUserByIdHandler` و `UpdateUserHandler` دیگر به شناسهٔ درخواست اعتماد نمی‌کنند: `ICurrentUserService` تزریق شد و فقط مالک پروفایل یا ادمین مجاز است (بررسی پیش از کوئری → عدم افشای وجود کاربر). الگوی `ServiceResult.Forbidden` از `CancelOrderHandler` کپی شد.
- **SEC-04 (مهم):** اندپوینت ناشناس `GET /admin/discounts/codes/{code}` از کنترلر ادمین حذف و به‌عنوان `GET /api/v{version}/discounts/codes/{code}` به کنترلر احرازویت‌شدهٔ `DiscountsController` منتقل شد (DTO حداقلی همان `DiscountInfoDto` باقی ماند).
- **SEC-05 (مهم):** `SecurityMiddleware` بازنویسی شد: (۱) مسیرهای versioned (`api/v{version}/admin/...`) هم تشخیص داده می‌شوند (قبلاً فقط `/api/admin` بررسی می‌شد که با هیچ مسیر واقعی مطابقت نداشت)؛ (۲) **fail-closed**: لیست سفید تنظیم‌شده ولی خالی ⇒ رد با ۴۰۳ + هشدار؛ (۳) نرمال‌سازی IPv4-mapped IPv6 (`::ffff:x.x.x.x`) برای سازگاری با پروکسی‌ها.
- **تست‌ها:** تست‌های موجود fixture شدند (تغییر رفتار امنیتی، بدون تضعیف assertion) + تست‌های جدید: Forbidden برای غیرادمین/غیرمالک، تست صفت `Roles=Admin` روی دو اکشن، تست‌های مسیر versioned/IPv4-mapped/fail-closed میدلور، تست انتقال‌یافتهٔ endpoint تخفیف.

### ✅ وضعیت اجرا — فاز ۲ پیاده‌سازی شد (۲۰۲۶-۱۰-۱۸)
- **SEC-06 (مهم):** اعتبارسنجی nonce وب‌هوک پرداخت **بی‌قید و شرط** شد (قبلاً پشت فلگ `Payment.Callback.SignatureRequired` بود؛ الگوی issue-at-initiation → callback URL از قبل کامل بود). parametr مسیر `{gateway}` به allowlist (`zarinpal`/`zarinpalsandbox`) تبدیل شد؛ در غیر آن ۴۰۰. میدلور IP در حالت «فلگ روشن + لیست خالی» fail-closed است (۴۰۳ + لاگ خطا). مقادیر production در `.env`/`.env.example` مستند شد: فلگ IP-whitelist روشن + `Zarinpal__AllowedIps` پر.
- **SEC-07 (مهم):** رفرش‌توکن فقط به‌صورت **هش SHA-256** ذخیره و جست‌وجو می‌شود (`RefreshTokenHasher` با مقایسهٔ constant-time؛ `UserSession.RefreshToken` → `RefreshTokenHash`؛ مهاجرت EF `HashRefreshTokenStorage`). مهاجرت ستون قدیمی را حذف می‌کند → نشست‌های موجود باطل و کاربران یک‌بار باید دوباره وارد شوند (پیامد امنیتی مورد نظر).
- **SEC-08 (مهم):** هش OTP با **HMAC-SHA256 با pepper سرور** (`Auth:OtpPepper` از `.env`، حداقل ۱۶ کاراکتر، غیر Present → fail-fast). حملهٔ آفلن کرک ۱۰⁶ حالتِ کد ۶ رقمی با افشای دیتابیس بسته شد.
- **SEC-09 (مهم):** `AuthOptions` برای اولین بار به بخش `Auth` **bind** شد (قبلاً هرگز bind نمی‌شد و همهٔ تنظیمات آن مرده بودند!) و قفل حساب در `VerifyOtpHandler` و `GoogleLoginHandler` اعمال می‌شود: کاربر قفل‌شده → `Forbidden` پیش از هر بررسی؛ کد نامعتبر → `RecordFailedLogin` (پارامتریک از کانفیگ)؛ کد معتبر → پاک‌سازی شمارنده.
- **SEC-10 (مهم):** `ISessionRevocationChecker` + کش ۳۰ ثانیه‌ای (منفی ۱۰ ثانیه) در `OnTokenValidated` از طریق claim `sid` — نشست‌های ابطال‌شده (logout-all، تغییر رمز، ابطال ادمین) ظرف ≤۱۰ ثانیه از کار می‌افتند؛ عمر access token از ۶۰ به ۱۵ دقیقه کاهش یافت.
- **تست‌ها:** ۲۵ تست جدید/به‌روز (nonce بی‌قید‌وشرط، هش رفرش‌توکن، pepper OTP و گاردهایش، قفل حساب، چکر ابطال با کش) + fixture تست‌های موجود.

### ✅ وضعیت اجرا — فاز ۳ پیاده‌سازی شد (۲۰۲۶-۱۰-۱۸)
- **SEC-11 (متوسط):** `ResolveFrontendBaseUrl` دیگر هدر `Origin` خام را دنبال نمی‌کند؛ فقط originهای allow-listشده (`Security:AllowedOrigins` — همان لیست CORS) پذیرفته می‌شوند، وگرنه ریدایرکت به `FrontendUrls:BaseUrl` انجام می‌شود. `SecuritySettings.AllowedOrigins` اضافه شد.
- **SEC-12 (متوسط):** CORS از `AllowAnyHeader/AllowAnyMethod` به لیست‌های صریح رفت (متدها: GET/POST/PUT/DELETE/PATCH؛ هدرها: Content-Type، Authorization، Accept، Origin، X-Requested-With، X-XSRF-TOKEN) — originها همچنان از کانفیگ.
- **SEC-13 (متوسط):** نام فایل آپلود کاملاً سمت سرور تولید می‌شود (GUID + پسوند از allow-list؛ نامعتبر → `.bin`) — نام کلاینت هرگز به کلید S3/URL عمومی نمی‌رسد. در برند نیز پسوند از content-type اعتبارسنجی‌شده مشتق می‌شود (jpeg/png/webp).
- **SEC-14 (متوسط):** `WalletUserResolver` معرفی شد: غیرادمین همیشه به شناسهٔ خودش resolve می‌شود (درخواستِ او نادیده)؛ ادمین می‌تواند کاربر هدف را بدهد؛ «بدون فیلتر» (null) برای کوئری‌های تجمعی ادمین حفظ شد. در ۶ هندلر کیف پول اعمال شد.
- **SEC-15 (متوسط):** آی‌پی تهی دیگر rate limit را دور نمی‌زند؛ در باکت مشترک `rl_ip_unknown` با سقف تشدیدشدهٔ ۳۰ در دقیقه قرار می‌گیرد. (کلیدهای global عمداً حفظ شد — امن‌تر از کلید per-endpoint.)
- **SEC-16 (متوسط):** `[AllowAnonymous]` از `notifications/unread-count` حذف شد.
- **تست‌ها:** ۹ تست جدید (۲ ریدایرکت origin، ۵ resolver، ۲ S3) + به‌روزرسانی تست‌های متأثر (CORS بدون wildcard، bucket ناشناس، عدم AllowAnonymous، نام سمت سرور). مسیرهای export/pending کیف پول برای هدف نامعتبر به‌جای DomainException مقدار `Unauthorized` برمی‌گردانند (دقیق‌تر).

### ✅ وضعیت اجرا — فاز ۴ پیاده‌سازی شد (۲۰۲۶-۱۰-۱۸)
- **SEC-17 (کم):** `Presentation/appsettings.Development.json` از Git خارج شد (در `.gitignore` نگهبان صریح اضافه شد) و همهٔ مقادیر حساس داخلش (`Jwt:Key`، `Kavenegar:ApiKey`، `ConnectionStrings:*`، `Elasticsearch:Password`) خالی ماند؛ برای محیط محلی `.env` یا `dotnet user-secrets` استفاده می‌شود. `appsettings.Production.json` از فاز ۰ gitignored است.
- **SEC-18 (کم):** `docker-compose.yml` اصلاح شد: `dockerfile: Presentation/Dockerfile` (مسیر قبلی `MainApi/Dockerfile` وجود نداشت)؛ `ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Production}` با امکان override از `.env`؛ `Encrypt=True` برای اتصال دیتابیس با توضیح صریح دربارهٔ `TrustServerCertificate` (فقط گواهی self-signed کانتینر محلی و الزام CA معتبر در استقرار واقعی)؛ انتظار سلامت دیتابیس (`condition: service_healthy`) برای `db-init` و `api`؛ رمز `sqlcmd` از طریق `SQLCMDPASSWORD` (نه `-P`) تا در process list دیده نشود. `Presentation/Dockerfile` نیز بازنویسی شد: restore چندپروژه‌ای، publish واقعی `Presentation.csproj`، healthcheck روی `/health/live` و نام DLL درست.
- **SEC-19 (کم):** رشته‌های اتصال production دیگر در هیچ فایل ردیابی‌شده‌ای وجود ندارند (حذف `otp.json`/`appsettings.Production.json` در فاز ۰)؛ قالب `.env.example` تنها اتصال localhost دارد و `.env` واقعی gitignored است. تأیید TLS سرویس‌های ابری به چک‌لیست استقرار فاز ۵ منتقل شد.
- **SEC-20 (کم):** `AllowedHosts` از `*` به `ledka-co.ir;www.ledka-co.ir;api.ledka-co.ir;localhost;127.0.0.1` محدود شد (دفاع در برابر Host header injection).
- **SEC-21 (کم):** `SendOtpHandler` دیگر `TimeSpan.FromMinutes(2)` هاردکد ندارد: طول و عمر OTP از `Auth:OtpLength` و `Auth:OtpExpirationMinutes` خوانده می‌شود. کلاس مردهٔ `OtpOptions` (که هرگز با مقادیر واقعی هم‌خوان نبود) حذف و `AuthOptions` منبع واحد شد؛ `ConfigController` همان مقادیر را به کلاینت گزارش می‌کند.
- **SEC-22 (کم/اطلاعاتی):** `CloseTicketRequest` (مرده و حاوی `IsAdmin` از سمت کلاینت) حذف شد؛ `GetTicketQuery` دیگر `RequestingUserId`/`IsAdmin` را در payload نمی‌پذیرد و `GetTicketHandler` هویت را از `ICurrentUserService` می‌خواند: فقط مالک یا ادمین مجاز است (تست: بدون claim → `Forbidden`).
- **SEC-23 (اطلاعاتی):** `MockGatewayController` مقادیر ورودی (`orderId`) را با `HtmlEncoder` برای context متن و attribute انکود می‌کند و همچنان فقط در Development قابل دسترسی است.
- **SEC-24 (اطلاعاتی):** ثبت‌نام «اول OTP» به‌عنوان تصمیم طراحی محصول در `SendOtpHandler` مستند شد (پاسخ یکسان، بدون افشای وجود شماره).
- **SEC-25 (اطلاعاتی):** `ValidateInitialAdmin` در استارت اضافه شد: اگر `InitialAdmin:PhoneNumbers` در Production پر باشد، هشدار عملیاتی لاگ می‌شود (لیست در `appsettings.json` خالی است و از env تأمین می‌شود).
- **تست‌ها/تأیید:** `dotnet build -c Release` با ۰ خطا/۰ هشدار؛ ۱۱٬۵۵۵ تست (۱۱٬۵۲۱ baseline فاز ۳ + ۳۴ تست جدید/به‌روزشده). تست‌های جدید/به‌روزشده: عمر OTP از کانفیگ، هویت تیکت از claims، `ConfigController` بدون `OtpOptions` و fixture شدن `PaymentService` پس از حذف `IFeatureManager` غیرضروری.

### ✅ وضعیت اجرا — فاز ۵ پیاده‌سازی شد (۲۰۲۶-۱۰-۱۸)
- **ابزار تأیید یک‌فرمانی:** `scripts/security/verify-security.ps1` هفت بررسی را پشت‌سرهم اجرا می‌کند (hygiene مخزن، placeholderهای appsettings، `dotnet build -warnaserror`، کل تست‌ها، `dotnet list package --vulnerable`، `gitleaks dir`، `gitleaks git`) و با جدول PASS/FAIL/PENDING و کد خروج غیرصفر در شکست تمام می‌شود. نسخهٔ gitleaks (`8.30.1`) با SHA-256 رسمی پین و در صورت نبود به‌صورت خودکار دانلود/اعتبارسنجی می‌شود. گزینهٔ `-FailOnHistoricalSecrets` برای گیت انتشار اضافه شد.
- **CI واقعی (نقص جدی قبلی):** `.github/workflows/ci.yml` یک **فایل اکسل خراب** بود (۶٬۱۸۵ بایت با امضای `PK` در commit «phase 0 complete») و بنابراین هیچ CI‌ای وجود نداشت. با workflow چهار-جابی جایگزین شد: `secret-scan` (gitleaks روی بازهٔ commitهای push/PR، blocking)، `secret-history-scan` (کل تاریخچه، هفتگی/دستی)، `build-test` (`dotnet build -warnaserror` + کل تست‌ها روی runner دارای Docker) و `dependency-audit` (شکست در صورت بستهٔ آسیب‌پذیر). `.github/dependabot.yml` هم برای NuGet/Actions/Docker اضافه شد.
- **gitleaks:** `.gitleaks.toml` بازنویسی شد. نسخهٔ قبلی با lookahead (`(?!...)`) روی موتور RE2 با `MustCompile` **کرش می‌کرد** و عملاً هرگز کار نمی‌کرد. اکنون ۹ قاعدهٔ پروژه‌ای (کلید JWT لو‌رفته، کلیدهای ابر آروان، کاوه‌نگار، MerchantId زرین‌پال، پسورد در رشتهٔ اتصال، URI پستگرس، ردیابی `appsettings.Production/Staging.json` و `otp.json`) با allowlistهای دقیق و بدون تضعیف قواعد پیش‌فرض. اسکن درخت کاری روی «فایل‌های قابل commit» (tracked + untracked غیر ignore) انجام می‌شود تا رازهای محلی gitignored نویز نسازند.
- **pre-commit:** `.githooks/pre-commit` نسخه‌بندی‌شده + `scripts/security/install-git-hooks.ps1` (بدون نیاز به pre-commit یا Go): ابتدا فایل‌های `.env`/`appsettings.Production.json`/`otp.json` را مسدود می‌کند، سپس gitleaks و در نبود آن `scripts/security/scan-secrets.ps1` (اسکنر PowerShell بدون وابستگی) را روی staged diff اجرا می‌کند. `.pre-commit-config.yaml` نیز به gitleaks v8.30.1 و hook مسدودسازی فایل‌های محیطی به‌روز شد. هر دو مسیر با یک راز جعلی آزمون شدند و commit را بلاک کردند.
- **چرخش رازها و بازنویسی تاریخچه:** رویهٔ گام‌به‌گام در `docs/07-operations/security-verification.md` + اسکریپت `scripts/security/purge-git-history.ps1` (پیش‌فرض dry-run، بکاپ `git bundle`، حذف مسیرهای لو‌رفته و redaction خودکار مقادیری که gitleaks گزارش می‌کند، بدون چاپ راز). **این دو مرحله عمداً اجرا نشده‌اند** چون نیازمند اعتبارنامه/تصمیم شما و force-push هماهنگ‌شده با همکاران است.
- **گیت‌های استارت Production (`ValidateProductionHardening`):** رد کلید JWT با الگوی لو‌رفته (SEC-01)، الزام `Auth:OtpPepper` ≥۱۶ کاراکتر (SEC-08)، رد `AllowedHosts: *` (SEC-20)، الزام `Security:AdminIpWhitelist` و `Security:AllowedOrigins` غیرخالی و بدون wildcard (SEC-05/11/12)، اجبار `Payment.Callback.IpWhitelistRequired=true` + `Zarinpal:AllowedIps` غیرخالی (SEC-06) و هشدار برای `InitialAdmin` پر و `Swagger:Enabled=true`. همهٔ مشکلات یک‌جا گزارش می‌شوند. ۸ تست جدید (۲۹ تست در `ConfigurationValidationExtensionTests`).
- **زیرساخت تست:** `PostgresContainerFixture` ساخت lazy کانتینر را داخل `InitializeAsync` برد تا نبود Docker به «skip» تبدیل شود، نه خطای ساخت fixture (قبلاً ۱٬۵۳۱ تست در ماشین بدون Docker به‌صورت error شکست می‌خوردند).
- **اسناد:** `docs/07-operations/security-verification.md` جدید (تأیید، چرخش، بازنویسی تاریخچه، چک‌لیست انتشار، نگهداری مستمر)؛ `security.md` و `deployment.md` با واقعیت جدید (Dockerfile/compose فاز ۴، گیت‌های استارت، gitleaks v8.30.1) هم‌راستا شدند و فهرست `docs/README.md` تمدید شد.
- **نتایج تأیید (اجرای واقعی):** build با `-warnaserror` PASS (۰ خطا/۰ هشدار)؛ ۱۱٬۵۶۵ تست (۱۰٬۰۳۴ پاس + ۱٬۵۳۱ اسکیپ دیتابیسی بدون Docker؛ در CI دارای Docker همه اجرا می‌شوند)؛ هیچ بستهٔ آسیب‌پذیر NuGet؛ `gitleaks dir` صفر یافته؛ `gitleaks git` **۱۴۱ یافتهٔ تاریخی (PENDING)** که پس از بازنویسی تاریخچه به صفر می‌رسد.
- **اقدامات دستی باقی‌مانده:** (۱) چرخش اعتبارنامه‌های SEC-01، (۲) `powershell -File scripts/security/purge-git-history.ps1 -Execute` و force-push هماهنگ‌شده، (۳) سپس `verify-security.ps1 -FailOnHistoricalSecrets` باید PASS شود.

### ✅ وضعیت اجرا — فاز ۶ پیاده‌سازی شد (۲۰۲۶-۱۰-۱۸)
- **نگهبان‌های خودکار ده اصل:** ۳۱ تست در `Tests/Security/` هر نقطهٔ قوت را به یک assertion تبدیل کرد (BCrypt work factor ۱۲، CSPRNG + pepper برای OTP، کوکی `__Host-refresh` و Anti-forgery، هدرهای امنیتی با nonce، pin شدن HS256 و ClockSkew ۳۰ ثانیه، AES-GCM با تشخیص دستکاری و nonce یکتا، مبلغ سمت سرور) و ۳ تست تکمیلی در مجموعه‌های موجود (۲ برای AES-GCM، ۱ برای مبلغ پرداخت).
- **اسکن منبع (`SourceHygieneInvariantsTests`):** هفت نگهبان روی کد production: نبود MD5/SHA1/3DES/`BinaryFormatter`/`new Random`، نبود SQL خامِ interpolateشده، نبود wildcardهای CORS، استفاده از `Connection.RemoteIpAddress` (و نه `X-Forwarded-For`) در rate limit، نبود لاگ راز/هدر Authorization، وابسته نبودن nonce وب‌هوک به فلگ، و نبود `[AllowAnonymous]` روی unread-count.
- **نقص کشف‌شده و رفع‌شده:** `EnableNonce`/`EnableCoop`/`EnableCoep`/`EnableCorp` پیش‌فرض `false` بودند و در `appsettings.json` تنظیم نشده بودند؛ یعنی CSP nonce و COOP/COEP/CORP — که فاز ۶ به‌عنوان قوت موجود فهرست کرده بود — در عمل ارسال نمی‌شدند. اکنون در بخش `SecurityHeaders` فعال شده‌اند و `SecurityHeadersInvariantsTests` ارسال واقعی چهار هدر و nonce یکتا در هر درخواست را تأیید می‌کند.
- **قفل پیکربندی:** `SecurityConfigurationInvariantsTests` وضعیت commit‌شده را نگه می‌دارد: بدون راز زنده در appsettings، `AllowedHosts` بدون `*`، whitelist ادمین و originهای مجاز غیرخالی/بدون wildcard، `EnableMockGateway=false` و روشن بودن فلگ امضای وب‌هوک.
- **گیت اجرایی:** `scripts/security/verify-security.ps1` بررسی مستقل «security invariants (phase 6)» گرفت (فیلتر `Tests.Security.*`) که پیش از کل تست‌ها اجرا می‌شود؛ CI هم با اجرای کل مجموعه هر تضعیف را قرمز می‌کند.
- **بازبینی دوره‌ای:** سند `docs/07-operations/security-invariants.md` (ده اصل + نگهبان + پروتکل تغییر + چک‌لیست هر ۶ ماه) و قالب Issue `.github/ISSUE_TEMPLATE/security-review.md`؛ لینک‌ها در `security.md` و `docs/README.md` اضافه شد.
- **تأیید:** build با `-warnaserror` (۰ خطا/۰ هشدار)؛ ۳۴ تست جدید فاز ۶ پاس (۳۱ در `Tests.Security` + ۳ تکمیلی)؛ کل تست‌ها ۱۱٬۵۹۹ مورد (۱۰٬۰۶۸ پاس + ۱٬۵۳۱ اسکیپ دیتابیسی بدون Docker) بدون رگرسیون؛ `verify-security.ps1` با گیت جدید «security invariants (phase 6)» پاس می‌شود.

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
- `Presentation/appsettings.json:119` — کلید امضای JWT به‌صورت متن ساده (۴۳ کاراکتر با الگوی `Breaking…Bad!…`؛ مقدار عمداً در این گزارش نقل نشده — پس از فاز ۰ در `.env` نگهداری می‌شود)
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
- `dotnet build -warnaserror` + اجرای کامل تست‌ها (بدون رگرسیون — تست‌های فعلی حفظ شوند).
- تست‌های امنیتی جدید: ۴۰۳ برای SEC-02/03/05؛ هش توکن (SEC-07)؛ fail-closed وبهوک (SEC-06).
- اسکن: `dotnet list package --vulnerable`؛ `gitleaks detect`؛ `npm audit` (اگر اگر前端 جدا است)؛ docker `trivy image` (در محیط دارای Docker).
- بررسی دستی: نیازمند تغییرات کانفیگ production (کلیدهای rotated، فلگ‌ها روشن، allow-listها پر) پیش از استقرار.

**وضعیت اجرای چک‌لیست (فاز ۵):** همهٔ موارد بالا خودکار شده‌اند: `scripts/security/verify-security.ps1` (اجرای محلی)
و `.github/workflows/ci.yml` (چهار job: اسکن اسرار بازهٔ commit، اسکن کل تاریخچه، build+test، dependency audit).
تنها اقلام باقی‌مانده، اقدامات دستی SEC-01 هستند: **چرخش اعتبارنامه‌ها**، **بازنویسی تاریخچهٔ Git**
(`scripts/security/purge-git-history.ps1`) و سپس تأیید با `verify-security.ps1 -FailOnHistoricalSecrets`.
رویهٔ کامل و چک‌لیست انتشار در [`docs/07-operations/security-verification.md`](docs/07-operations/security-verification.md) است.

---

## فاز ۶ — نقاط قوت امنیتی موجود (حفظ شود)

این موارد در پروژه دستکاری نمی‌شوند و از فاز ۶ به بعد با تست و اسکن خودکار نگهبانی می‌شوند (`Tests/Security/` — سند: `docs/07-operations/security-invariants.md`):
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

*پایان گزارش — فازهای ۰ تا ۶ پیاده‌سازی شده‌اند و با تست/اسکن خودکار و گیت CI نگهبانی می‌شوند. تنها اقدامات دستی باقی‌مانده، چرخش اعتبارنامه‌های SEC-01 و بازنویسی تاریخچهٔ Git است (رویه: `docs/07-operations/security-verification.md`).*
