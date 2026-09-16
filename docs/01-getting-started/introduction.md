# معرفی پروژه

Mechanic API بک‌اند فروشگاهی اینترنتی روی .NET 9 است که با معماری Clean و CQRS ساخته شده و
۲۱ ماژول دامنه‌ای (از کاتالوگ محصول تا کیف پول و انبار) دارد. این سند هدف دامنه، فهرست
تکنولوژی‌های اصلی خوانده‌شده از فایل‌های csproj و شمارش پروژه‌های Solution را پوشش می‌دهد.

## هدف دامنه

کد این ریپو یک فروشگاه اینترنتی کامل را پوشش می‌دهد: مدیریت کاتالوگ (محصول، تنوع، دسته‌بندی، برند، ویژگی)،
فرایند خرید (سبد خرید، سفارش، پرداخت، تخفیف، ارسال)، انبارش و موجودی، کیف پول با تشخیص تقلب،
نظر و علاقه‌مندی، احراز هویت مبتنی بر OTP و سشن دستگاه، جست‌وجوی Elasticsearch و پشتیبانی تیکتی.
هر ماژول یک پوشه جدا در هر لایه دارد؛ فهرست کامل ماژول‌ها و نقشه اسناد آینده آن‌ها در [docs/README.md](../README.md) آمده است.

نام‌گذاری پروژه در کد یکدست نیست: عنوان Swagger «Ledka API» و نام برنامه در لاگ Serilog «Mechanic.Api» است.
این سند از نام «Mechanic API» استفاده می‌کند.

## تکنولوژی‌ها و وابستگی‌های کلیدی

مقادیر زیر مستقیماً از فایل‌های csproj استخراج شده‌اند. همه پروژه‌ها `net9.0` با
`Nullable=enable` و `ImplicitUsings=enable` هدف‌گذاری شده‌اند.

| فناوری | پکیج و نسخه | نقش |
|---|---|---|
| .NET SDK | `9.0.314` (در `global.json` با `rollForward: latestFeature`) | پلتفرم اجرا |
| ASP.NET Core | `Microsoft.NET.Sdk.Web` در `Presentation/Presentation.csproj` | میزبانی API با کنترلرهای attribute-routed |
| EF Core | `Microsoft.EntityFrameworkCore` 9.0.17 + `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.0 | ORM روی PostgreSQL |
| Dapper | `Dapper` 2.1.66 | کوئری‌های سبک ADO.NET (`Application/Common/Contracts/ISqlConnectionFactory.cs`) |
| MediatR | `MediatR` 14.0.0 | مدیاتور Command/Query و انتشار Domain Event |
| FluentValidation | `FluentValidation` 12.1.1 | اعتبارسنجی ورودی Command/Query |
| Mapster | `Mapster` 10.0.7 | نگاشت دامنه به DTO |
| Redis | `StackExchange.Redis` 2.10.14 + `Microsoft.Extensions.Caching.StackExchangeRedis` 9.0.17 | کش، قفل توزیع‌شده، idempotency، ذخیره کلید DataProtection |
| Elasticsearch | `Elastic.Clients.Elasticsearch` 9.3.4 | موتور جست‌وجو با Outbox اختصاصی |
| AWS S3 | `AWSSDK.S3` 4.0.20.2 | ذخیره فایل و آرشیو حسابرسی |
| Kavenegar | `Kavenegar` 1.2.5 | ارسال SMS/OTP |
| زرین‌پال | پیاده‌سازی داخلی در `Infrastructure/Payment/ZarinPal` | درگاه پرداخت (در csproj پکیجی برای آن نیست) |
| احراز هویت | `Microsoft.AspNetCore.Authentication.JwtBearer` و `.Google` 9.0.17 | JWT Bearer و OAuth گوگل |
| BCrypt | `BCrypt.Net-Next` 4.0.3 | هش پسورد (`Infrastructure/Security/Services/PasswordHasher.cs`) |
| Serilog | `Serilog.AspNetCore` 9.0.0 + Sinkهای Console/File | لاگ‌گیری ساختاریافته |
| OpenTelemetry | 1.17.0 با Exporter OTLP | Trace و متریک |
| Feature Flags | `Microsoft.FeatureManagement` 4.6.0 | سوییچ قابلیت‌ها (`SharedContracts/FeatureManagement`) |
| نسخه‌بندی API | `Asp.Versioning.Mvc` و `.ApiExplorer` 8.1.1 | نسخه‌بندی URL-segment |
| Swagger | `Swashbuckle.AspNetCore` 9.0.6 (+ Annotations) و `Scalar.AspNetCore` 1.2.3 | مستندسازی OpenAPI |
| Rate Limiting | `AspNetCoreRateLimit` 5.0.0 + `System.Threading.RateLimiting` داخلی | محدودسازی نرخ |
| Quartz | `Quartz` 3.13.0 | در csproj ارجاع شده اما در کد هیچ استفاده‌ای از آن یافت نشد؛ Jobها با `BackgroundService` نوشته شده‌اند |
| Key Vault | `Azure.Identity` 1.11.4 + `Azure.Security.KeyVault.Secrets` 4.6.0 | ارجاع در `Infrastructure/Infrastructure.csproj`؛ استفاده فعال از آن در کد فعلی مشخص نیست |

ابزارها: `dotnet-ef` 9.0.17 در manifest محلی (`dotnet-tools.json` و نسخه تکراری `.config/dotnet-tools.json`)،
gitleaks و pre-commit (جزئیات در [07-operations/security.md](../07-operations/security.md)).

## نمای کلی Solution

`API.sln` شامل ۷ پروژه است:

| پروژه | نوع | مسئولیت خلاصه |
|---|---|---|
| `SharedKernel` | Classlib | پایه‌های مشترک دامنه: `Entity<T>`، `ValueObject`، `Specification<T>`، `Result`، `Guard`، `DomainException` |
| `SharedContracts` | Classlib | قراردادهای فرا-لایه‌ای: `Diagnostics` (ActivitySourceها) و `FeatureManagement`؛ بدون وابستگی |
| `Domain` | Classlib | ۲۱ ماژول دامنه با Aggregate، ValueObject، Event و Exception |
| `Application` | Classlib | لایه کاربرد: Featureهای CQRS، Behaviors، Contract و Adapter |
| `Infrastructure` | Classlib | پیاده‌سازی‌ها: Persistence، Outbox، Redis، Elasticsearch، S3، Jobها، Security |
| `Presentation` | Web | کنترلرها، پاکت پاسخ، Middlewareها و Composition Root (`Program.cs`) |
| `Tests` | Test | تست واحد و یکپارچه برای همه لایه‌ها (جزئیات در [06-testing/testing-strategy.md](../06-testing/testing-strategy.md)) |

زنجیره وابستگی (جزئیات و نمودار کامل در [02-architecture/overview.md](../02-architecture/overview.md)):
`SharedKernel` و `SharedContracts` در هسته‌اند؛ `Domain` فقط به `SharedKernel`، `Application` به سه هسته‌ای،
`Infrastructure` به `Application` و سه هسته‌ای، و `Presentation` به `Application` و `Infrastructure` و `SharedContracts` وابسته است.

## گام بعدی

برای اجرای پروژه به [setup-and-run.md](setup-and-run.md) و برای فهم قواعد معماری به
[02-architecture/overview.md](../02-architecture/overview.md) بروید.
