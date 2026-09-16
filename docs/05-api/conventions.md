# قراردادهای سراسری REST API

این سند قواعد مشترک همه Endpointهای API را مستند می‌کند: مسیردهی و نسخه‌بندی، پاکت پاسخ،
مدیریت خطا با ProblemDetails فارسی، Middlewareها و فیلترها، Rate Limiting و Swagger.
جزئیات هر ماژول خاص به اسناد `03-modules/` (فاز بعدی داکیومنت‌سازی) موکول می‌شود.

## مسیردهی و نسخه‌بندی

- همه کنترلرها از `BaseApiController` ارث می‌برند (`Presentation/Base/Endpoints/v1/BaseApiController.cs`)
  که با `[ApiController]`، `[ApiVersion("1.0")]` و قالب پیش‌فرض
  `[Route("api/v{version:apiVersion}/[controller]")]` تعریف شده است.
- بسیاری از ماژول‌ها مسیر صریح با اسم جمع می‌دهند؛ مثلاً `ProductsController` با
  `[Route("api/v{version:apiVersion}/products")]` (`Presentation/Product/Endpoints/ProductsController.cs`)
  و مسیرهای مدیریتی الگوی `api/v{version}/admin/...` را دارند.
- نسخه‌بندی با `Asp.Versioning.Mvc` 8.1.1 و `UrlSegmentApiVersionReader` است: نسخه فقط در سگمنت URL،
  `DefaultApiVersion = 1.0`، `AssumeDefaultVersionWhenUnspecified = true` (بدون سگمنت هم v1 پاسخ می‌دهد)
  و `ReportApiVersions = true` (هدر گزارش نسخه‌های پشتیبانی‌شده). فعلاً فقط نسخه ۱.۰ وجود دارد
  و تنها پوشه نسخه‌دار، `Presentation/Base/Endpoints/v1/` است.
- Endpointهای دامنه همه MVC و attribute-routed هستند؛ Minimal API فقط برای Health Checkها و
  Endpoint ریشه (`GET /` با پاسخ `{ status: "Healthy", timestamp }`) استفاده شده است.

## مقیاس فعلی

شمارش grep روی فایل‌های `Presentation` (به‌جز bin/obj): **۵۶ کنترلر** (همگی ارث‌بری از `BaseApiController`)
و **۳۰۲ عملیات HTTP**:

| فعل | تعداد |
|---|---|
| `[HttpGet]` | ۱۳۶ |
| `[HttpPost]` | ۷۸ |
| `[HttpPatch]` | ۳۰ |
| `[HttpDelete]` | ۳۷ |
| `[HttpPut]` | ۲۱ |

نمونه امضای واقعی — `GET api/v1/products/{id}` در `ProductsController`:
اکشن `GetProduct(Guid id)` با `[HttpGet("{id:guid}")]`، ورودی لیست از Query با
`GetProductsRequest` (`Page`, `PageSize`, `Search`, `CategoryId`, `BrandId`, `MinPrice`, `MaxPrice`,
`InStockOnly`, `SortBy`) و پاسخ‌های مستندشده با `[ProducesResponseType]`.

## پاکت پاسخ

هر پاسخ موفق یا ناموفق از `HttpResultMapper` (`Presentation/Common/Mappers/HttpResultMapper.cs`)
عبور می‌کند و در یکی از رکوردهای `Presentation/Base/Responses/ApiResponse.cs` پیچیده می‌شود:

```csharp
public record ApiResponse<T>(T? Data, bool Success, string? Message, IDictionary<string, string[]>? Errors = null);
public record ApiResponse(bool Success, string? Message, IDictionary<string, string[]>? Errors = null);
public record PaginatedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage);
```

- خروجی JSON به‌صورت camelCase و با حذف nullها است.
- کلید `Errors` دیکشنری «نام ویژگی → آرایه پیام» است؛ برای خطاهای دامنه کلید جایگزین `domain` استفاده می‌شود.
- صفحات با `PaginatedResponse<T>` و برگرداندن `201 Created` با هدر `Location` از طریق `ToCreatedActionResult` ممکن است.
- نگاشت `ErrorType` به کد HTTP:

| ErrorType | کد HTTP |
|---|---|
| `Validation` | 400 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `BusinessRule` | 422 |
| `RateLimitExceeded` | 429 |
| بقیه (پیش‌فرض) | 500 |

## مدیریت خطا و ProblemDetails

- `PersianProblemDetails` (`Presentation/Common/ProblemDetails/PersianProblemDetails.cs`) پیاده‌سازی
  RFC 7807 با فیلدهای `type` (الگوی `https://ledka.ir/errors/{status}`)، `title`، `status`، `detail`،
  `instance`، `traceId`، `errorCode`، `errors` و `timestamp` است؛ عنوان‌ها و جزئیات فارسی‌اند.
- سازنده‌ها در `PersianProblemDetailsFactory` (متدهای `FromStatus`، `FromValidation`، `FromDomainException`) و
  مصرف‌کننده اصلی `CustomExceptionHandlerMiddleware` است که انواع `ValidationException`، `DomainException`،
  `KeyNotFoundException`، `UnauthorizedAccessException`، `ConcurrencyException` و ... را نگاشت می‌کند.
  Middleware مکمل `DomainExceptionTranslationMiddleware` هم خطاهای دامنه را ترجمه می‌کند.
- `ValidationFilter` فیلتر پیش‌فرض ModelState را با `SuppressModelStateInvalidFilter = true` غیرفعال و
  به‌جای آن پاسخ 400 با پاکت استاندارد و پیام فارسی «اطلاعات ورودی نامعتبر است.» برمی‌گرداند.

## ارسال توکن و هویت کاربر

- طرح احراز هویت Bearer استاندارد JWT است: هدر `Authorization: Bearer <jwt>` — تعریف Swagger با
  نام `Bearer`، `SecuritySchemeType.Http`، `Scheme = "bearer"` و `BearerFormat = "JWT"` ثبت شده و
  به‌صورت global روی همه عملیات اعمال می‌شود.
- هویت داخل درخواست با `ICurrentUserService` خوانده می‌شود (پیاده‌سازی
  `Presentation/Common/Services/CurrentUserService.cs` از روی `ClaimsPrincipal`):
  `UserId` از claimهای `nameid`/`sub`، `SessionId` از `sid`، `IsAdmin` از roleها و آدرس IP کلاینت.
- مسیرهای احراز هویت (`POST /auth/otp`، `POST /auth/otp/verify`، `POST /auth/token/refresh`،
  ورود گوگل و خروج سشن) در `Presentation/Auth/Endpoints/AuthController.cs` هستند؛ جریان کامل
  OTP/سشن موضوع سند آینده `03-modules/identity/auth-security.md` است.

## Middlewareها

ترتیب دقیق زنجیره در `Presentation/Common/Extensions/MiddlewareExtensions.cs` (متد `UseApplication`):

| ترتیب | Middleware | نقش |
|---|---|---|
| ۱ | `UseForwardedHeaders` | پشت Reverse Proxy |
| ۲ | `UseHttpsRedirection` | — |
| ۳ | `UseStaticFiles` | wwwroot (در حال حاضر خالی) |
| ۴ | `UseSwagger` + `UseSwaggerUI` | فقط وقتی `Swagger:Enabled`؛ RoutePrefix با `swagger` |
| ۵ | `CorrelationIdMiddleware` | تضمین هدر `X-Correlation-ID` |
| ۶ | `RequestLoggingMiddleware` | لاگ ساختاریافته درخواست |
| ۷ | `UseSerilogRequestLogging` | لاگ درخواست Serilog |
| ۸ | `CustomExceptionHandlerMiddleware` | تبدیل استثنا به ProblemDetails فارسی |
| ۹ | `SecurityHeadersMiddleware` | هدرهای امنیتی (CSP، HSTS و ...) |
| ۱۰ | `UseApplicationLocalization` | محلی‌سازی پیام‌ها |
| ۱۱ | `UseRequestPerformanceMonitoring` | پایش زمان درخواست |
| ۱۲ | `UseCustomCors` | سیاست `AllowClient` |
| ۱۳ | `UseChaosEngineering` | فقط غیرتولیدی (see [cross-cutting.md](../02-architecture/cross-cutting.md)) |
| ۱۴ | `RateLimitMiddleware` | لایه AspNetCoreRateLimit |
| ۱۵ | `UseAuthentication` / `UseAuthorization` | JWT |
| ۱۶ | `UseApplicationRateLimiter` | لایه `System.Threading.RateLimiting` |
| ۱۷ | `UseApplicationAntiforgery` | ضد جعل درخواست |
| ۱۸ | `SessionActivityMiddleware` | به‌روزرسانی آخرین فعالیت سشن کاربر |
| ۱۹ | `UseAdminIpWhitelist` / `WebhookIpWhitelistMiddleware` | محدودسازی IP ادمین و وب‌هوک پرداخت |
| ۲۰ | `MapApplicationHealthChecks` | Endpointهای `/health/*` |

## فیلترها

| فیلتر | مسیر | فعال‌سازی |
|---|---|---|
| `ValidationFilter` | `Presentation/Common/Filters/` | سراسری |
| `OtpRateLimitFilter` | همان پوشه | با اتریبیوت `[OtpRateLimit]` |
| `ReviewRateLimitFilter` | همان پوشه | با اتریبیوت `[ReviewRateLimit]` |
| `PaymentRateLimitFilter` / `PaymentRateLimitAttribute` | همان پوشه | اتریبیوت موجود است؛ ثبت سراسری ندارد |

## Rate Limiting

سه لایه مکمل:

1. `System.Threading.RateLimiting` داخلی با پالیسی `admin-wallet` (پنجره ثابت، ۶۰ درخواست در دقیقه
   به ازای کاربر/IP — `Presentation/Common/Extensions/RateLimitingExtensions.cs`).
2. میان‌افزار `AspNetCoreRateLimit` (ردیف ۱۴ جدول بالا).
3. فیلترهای اکشن اختصاصی (`OtpRateLimitFilter` و ...) برای Endpointهای حساس.

## Swagger و مشتقات OpenAPI

- Swashbuckle 9.0.6 با یک SwaggerDoc به ازای هر نسخه API، عنوان «Ledka API»، Annotations فعال و
  Operation Filterهای `RemoveVersionParameterOperationFilter`، `DefaultResponseOperationFilter` و
  `CustomOperationIdOperationFilter` و Schema Filter `NullableSchemaFilter`؛ دسترسی با فلگ `Swagger:Enabled`.
- `Scalar.AspNetCore` در csproj ارجاع شده اما `MapScalarApiReference` در Pipeline فراخوانی نمی‌شود.
- `Presentation/nswag.json` کلاینت TypeScript/Angular را از `swagger/v1/swagger.json` تولید و در
  مسیر پروژه فرانت‌اند (`FrontEnd/src/app/core/api/generated/api-client.ts`) می‌نویسد.
- فایل `Presentation/MainApi.http` همچنان قالب پیش‌فرض Visual Studio است (فقط یک درخواست
  `weatherforecast` نمونه با هاست `http://localhost:5188`) و درخواست واقعی ندارد.

## اسناد مرتبط

- پاکت پاسخ و نگاشت خطا در چرخه هندلر: [cqrs-features.md](../02-architecture/cqrs-features.md)
- Health Checkها و فهرست چک‌ها: [cross-cutting.md](../02-architecture/cross-cutting.md)
- هدرهای امنیتی، IP Whitelist و ابزارهای اسکن: [07-operations/security.md](../07-operations/security.md)
