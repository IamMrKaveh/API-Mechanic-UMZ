# نقاط قوت امنیتی و نگهبان‌های خودکار (فاز ۶)

این سند «فاز ۶» ممیزی امنیتی ([SECURITY-AUDIT-REPORT.md](../../SECURITY-AUDIT-REPORT.md)) را از یک
فهرست توصیه به یک **قرارداد قابل‌آزمون** تبدیل می‌کند: ده نقطهٔ قوت امنیتی پروژه، هر یک با یک یا چند
تست/اسکن خودکار که در صورت تضعیف، build را قرمز می‌کند.

> قاعدهٔ کلی: هیچ‌یک از موارد زیر نباید بدون به‌روزرسانی همین سند و گزارش ممیزی و **بدون** جایگزین
> امن‌تر تغییر کند. تست‌های این فاز بخشی از قرارداد هستند، نه assertionهای قابل حذف.

## ۱) ده اصل و نگهبان هر یک

| # | اصل امنیتی | محل پیاده‌سازی | نگهبان خودکار |
|---|---|---|---|
| ۱ | هش رمز با BCrypt، work factor ۱۲ | `Infrastructure/Security/Services/PasswordHasher.cs` | `CryptoAndTokenInvariantsTests.PasswordHasher_KeepsBcryptWorkFactorAtTwelve` و `..._EmitsCostTwelveHashes` |
| ۲ | OTP با CSPRNG و ذخیرهٔ هش HMAC با pepper سرور | `Domain/Security/ValueObjects/OtpCode.cs`، `Infrastructure/Auth/Services/OtpService.cs` | `OtpCode_Generate_ProducesSixDigitCodesWithHighEntropy`، `OtpService_HashesWithServerPepper...`، `OtpService_WithMissingPepper_FailsFast`، `OtpCode_MatchesHash_...` |
| ۳ | کوکی رفرش `__Host-refresh` (HttpOnly/Secure/SameSite=Strict) + چرخش توکن + Anti-forgery | `Presentation/Common/Cookies/AuthCookieService.cs`، `Presentation/Auth/Endpoints/AuthController.cs` | `SessionCookieInvariantsTests` (۶ حالت در ۴ تست: پیکربندی، نوشتن، پاک‌کردن، attributeها) |
| ۴ | هدرهای امنیتی: HSTS، CSP با nonce، X-Frame، Referrer، Permissions، COOP/COEP/CORP | `Presentation/Common/Middleware/SecurityHeadersMiddleware.cs` + `SecurityHeaders` در appsettings | `SecurityHeadersInvariantsTests` (۴ تست) و `SecurityConfigurationInvariantsTests.SecurityHeaders_StayEnabled` |
| ۵ | پارامتری‌سازی کامل SQL (بدون رشتهٔ خام) | لایهٔ Persistence (EF Core) | `SourceHygieneInvariantsTests.ProductionCode_DoesNotInterpolateRawSql` |
| ۶ | تأیید مبلغ بر پایهٔ رکورد سمت سرور | `Infrastructure/Payment/Services/PaymentService.cs`، `CompleteWalletTopUpHandler` | `PaymentServiceTests.VerifyPaymentAsync_PassesTheServerSideTransactionAmountToTheGateway` |
| ۷ | اعتبارسنجی سختگیرانهٔ JWT (alg = HS256، issuer/audience/lifetime، ClockSkew ۳۰ ثانیه) | `Presentation/Common/Extensions/AuthenticationExtensions.cs` | `CryptoAndTokenInvariantsTests.JwtValidation_PinsHmacSha256AndStrictLifetimeChecks` (+ `AuthenticationExtensionsTests`) |
| ۸ | رمزنگاری کش Redis با AES-GCM (nonce یکتا، احراز اصالت) | `Infrastructure/Cache/Redis/Services/EncryptedRedisCacheService.cs` | `EncryptedRedisCacheServiceTests` (roundtrip، tag دستکاری‌شده، nonce یکتا) |
| ۹ | بدون رمزنگاری ضعیف، بدون `BinaryFormatter`/`new Random`، بدون لاگ راز | کل کد production | `SourceHygieneInvariantsTests.ProductionCode_DoesNotUseWeakCryptographyOrBinaryFormatter` و `..._DoesNotLogSecretsOrAuthorizationHeaders` |
| ۱۰ | Rate limit بر پایهٔ IP اتصال (نه `X-Forwarded-For` قابل جعل) | `Presentation/Common/Middleware/RateLimitMiddleware.cs` | `SourceHygieneInvariantsTests.RateLimitMiddleware_BindsToConnectionIpAndIgnoresForwardedHeaders` |

نگهبان‌های تکمیلی:

- `PaymentCallbackNonce_IsNotBehindAFeatureFlag` — nonce وب‌هوک پرداخت هرگز پشت فلگ برنمی‌گردد (SEC-06).
- `UnreadNotificationCount_IsNotAnonymous` — اندپوینت unread-count بدون `[AllowAnonymous]` می‌ماند (SEC-16).
- `SecurityConfigurationInvariantsTests` — وضعیت پیکربندی commit‌شده: بدون راز در `appsettings.json`، `AllowedHosts`
  بدون wildcard، whitelist ادمین و originهای مجاز غیرخالی، `EnableMockGateway=false`.
- `ProductionCode_DoesNotUseCorsWildcards` — بدون `AllowAnyOrigin/Header/Method` (SEC-12).

## ۲) اجرای نگهبان‌ها

```powershell
# فقط مجموعهٔ فاز ۶ (سریع، دقیقاً همین تست‌ها)
dotnet test Tests/Tests.csproj -c Release --filter "FullyQualifiedName~Tests.Security."

# کل زنجیرهٔ تأیید (شامل همین گیت، پیش از اجرای کل تست‌ها)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/verify-security.ps1
```

در CI، مجموعهٔ کامل تست‌ها در job `build-test` اجرا می‌شود و هر تضعیف این ده اصل باعث شکست build می‌شود؛
`verify-security.ps1` هم یک گام مستقل «security invariants (phase 6)» دارد تا خرابی سریع و با
نام فایل/خط دقیق دیده شود.

نکتهٔ اجرایی: تست‌های اسکن منبع (`SourceHygieneInvariantsTests`) ریشهٔ مخزن را از محل `API.sln` پیدا می‌کنند
و فقط پوشه‌های production (`Application`, `Domain`, `Infrastructure`, `Presentation`, `SharedKernel`,
`SharedContracts`) را می‌خوانند؛ `bin/obj` و پوشهٔ `Tests` مستثنا هستند.

## ۳) پروتکل تغییر

اگر تغییری لازم است:

1. در همان commit، `SECURITY-AUDIT-REPORT.md` (بخش فاز ۶) و همین سند را به‌روزرسانی کنید.
2. تست نگهبان را **جایگزین** کنید (assertion امن‌تر)، نه حذف؛ حذف یا `[Skip]` کردن تست = شکستن قرارداد.
3. دلیل امنیتی تغییر را در پیام commit یا PR بنویسید.

## ۴) بازبینی دوره‌ای (هر ۶ ماه یا پیش از انتشار Major)

| مورد | چطور | مرجع |
|---|---|---|
| اجرای گیت کامل | `verify-security.ps1 -FailOnHistoricalSecrets` | [security-verification.md](security-verification.md) |
| تاریخچهٔ Git پاک | job هفتگی `secret-history-scan` سبز باشد | همان |
| چرخش کلیدها | JWT / OTP Pepper / کلیدهای سرویس | [security-verification.md](security-verification.md) |
| بسته‌های آسیب‌پذیر | job `dependency-audit` و PRهای Dependabot | `.github/workflows/ci.yml` |
| بازبینی OWASP Top 10 | مرور دستی گزارش ممیزی و کد مسیرهای حساس | [SECURITY-AUDIT-REPORT.md](../../SECURITY-AUDIT-REPORT.md) |
| به‌روزبودن این فهرست | بررسی تطابق با کد فعلی | همین سند |

قالب Issue برای همین بازبینی: `.github/ISSUE_TEMPLATE/security-review.md`.

## اسناد مرتبط

- گزارش ممیزی و وضعیت فازها: [SECURITY-AUDIT-REPORT.md](../../SECURITY-AUDIT-REPORT.md)
- تأیید، چرخش رازها و چک‌لیست انتشار: [security-verification.md](security-verification.md)
- امنیت عملیاتی و gitleaks: [security.md](security.md)
