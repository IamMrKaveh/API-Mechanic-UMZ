# استراتژی تست

پروژه `Tests` به ازای هر لایه پوشه اختصاصی دارد و روی xUnit با الگوی «تست واحد بدون وابستگی شبکه +
تست یکپارچه روی PostgreSQL واقعی با Testcontainers» کار می‌کند. این سند ساختار، زیرساخت مشترک
(`TestInfrastructure`)، آمار واقعی فایل‌ها و نحوه اجرای تست‌ها را مستند می‌کند.

## ساختار و آمار

شمارش با `git ls-files Tests` انجام شده (bin/obj ردیابی نمی‌شوند) و با شمارش فایل‌سیستم تقاطع‌سنجی شده است:

| پوشه | تعداد فایل .cs | از این تعداد `*Tests.cs` |
|---|---|---|
| `Tests/Application` | ۴۵۷ | ۴۵۷ |
| `Tests/Infrastructure` | ۲۹۲ | ۲۹۱ (+ ۱ فایل کمکی `SkippableTheoryAttribute.cs`) |
| `Tests/Domain` | ۱۹۰ | ۱۹۰ |
| `Tests/TestInfrastructure` | ۶۲ | ۰ (زیرساخت، نه تست) |
| `Tests/Presentation` | ۴۱ | ۴۱ |
| `Tests/SharedKernel` | ۳۱ | ۳۱ |
| `Tests/Integration` | ۱۱ | ۱۱ |
| ریشه Tests (`Imports.cs`) | ۱ | ۰ |
| **جمع** | **۱٬۰۸۵** | **۱٬۰۲۱** |

شمارش attributeها روی کل پروژه: **۷٬۳۰۶ [Fact]** و **۶۸۱ [Theory]** (مجموعاً حدود ۷٬۹۸۷ testcase تعریف‌شده).
بزرگ‌ترین نواحی تست Application به ترتیب: Wallet (۷۴)، Review (۴۶)، Order (۳۸)، Inventory (۳۵)، Payment (۲۵)، Product (۲۴)، Auth (۲۱).

## فریم‌ورک‌ها و پکیج‌ها

| پکیج | نسخه | نقش |
|---|---|---|
| `xunit` + `xunit.runner.visualstudio` | 2.9.2 / 2.8.2 | فریم‌ورک و رانر |
| `Shouldly` | 4.3.0 | Assertion (بدون FluentAssertions) |
| `NSubstitute` | 6.0.0 | Mock و Stub |
| `Bogus` | 35.6.5 | تولید داده تستی |
| `Testcontainers.PostgreSql` | 4.13.0 | کانتینر PostgreSQL واقعی |
| `Respawn` | 7.0.0 | ریست دیتابیس بین تست‌ها |
| `Xunit.SkippableFact` | 1.5.61 | Skip پویا وقتی Docker در دسترس نیست |
| `coverlet.collector` | 6.0.2 | پوشش کد |
| `Microsoft.AspNetCore.Mvc.Testing` | 9.0.18 | ارجاع شده اما استفاده نمی‌شود — هیچ `WebApplicationFactory` یا تست HTTP در ریپو وجود ندارد |

## تست واحد به تفکیک لایه

| لایه | تمرکز | مثال واقعی |
|---|---|---|
| Domain | رفتار Aggregateها و ValueObjectها | `Tests/Domain/Brand/Aggregates/BrandTests.cs` — تست `Brand.Create` با `BrandBuilder` و `StubBrandUniquenessChecker` (کنترل `CallCount`) |
| Application | هندلرها و Validatorها | `Tests/Application/Brand/Features/Commands/CreateBrand/CreateBrandHandlerTests.cs` — Mockهای NSubstitute از `IBrandRepository` و ... و کنترل مسیرهای NotFound/Conflict با `ShouldFailWith` |
| Infrastructure | ریپازیتوری‌ها و سرویس‌ها (واحد) | تست‌های `Tests/Infrastructure` روی کلاس‌های ایزوله |
| Presentation | Middleware، فیلترها، ProblemDetails، Swagger | ۴۱ فایل تست واحد با NSubstitute |
| SharedKernel | Guard، Result، Specification و ابزارها | ۳۱ فایل |

الگوی رایج: به ازای هر Feature دو فایل `*HandlerTests` و `*ValidatorTests` وجود دارد.

## TestInfrastructure

زیرساخت مشترک در `Tests/TestInfrastructure` با پوشه‌های زیر:

| پوشه | محتوا |
|---|---|
| `Base/` | `IntegrationTestBase` — پایه `IAsyncLifetime` که `PostgresContainerFixture` را می‌گیرد، با `Skip.IfNot(Fixture.IsDockerAvailable)` بدون Docker رد می‌شود، `DBContext` واقعی می‌سازد و Seed helperهای `SeedCategoryAsync`/`SeedBrandAsync`/`SeedUserAsync` دارد. بعضی تست‌های یکپارچه به‌جای ارث‌بری، خودشان `IAsyncLifetime` را پیاده می‌کنند |
| `Builders/` | ۴۹ فایل Builder روان برای Aggregate و VO: `BrandBuilder`، `CategoryBuilder`، `ProductBuilder`، `OrderBuilder`، `WalletBuilder` و ... |
| `Fakes/` | Fakeهای رفتاری: `FakeElasticsearchServer` (سرور جعلی TCP با Router پاسخ)، `FakeHttpMessageHandler` (HttpClient اسکریپت‌پذیر)، `FakeLockHandle`، `FakeOrderPaymentContext` |
| `Stubs/` | Stubهای دستی با شمارش فراخوانی: `StubBrandUniquenessChecker`، `StubCategoryUniquenessChecker`، `StubAttributeTypeUniquenessChecker` |
| `Assertions/` | `ServiceResultAssertions` — اکستنشن‌های Shouldly مثل `ShouldBeSuccess` و `ShouldFailWith(errorCode)` |
| `Fixtures/` | `AutoMapperFixture` — نامش میراثی است؛ در واقع `TypeAdapterConfig` مپستر را Scan و `IMapper` می‌سازد |
| `Mapping/` | `MapsterConfigFixture` برای تست‌های نگاشت |
| `Helpers/` | `CatalogSeeder` — Seed دسته/برند/محصول واقعی در `DBContext` |
| `Database/` | `PostgresContainerFixture` (Testcontainers با `postgres:16-alpine`، کاربر/پایگاه `mechanic/mechanic_tests`) و `DatabaseCollection` — **توجه:** این پوشه با قاعده عام `Database/` در `.gitignore` افتاده و در git ردیابی نمی‌شود |

## تست یکپارچه

- سطح تست، **ریپازیتوری/Persistence** است نه HTTP: ریپازیتوری واقعی Infrastructure روی `DBContext` واقعی
  (با همان `AuditableEntityInterceptor` و `DomainEventInterceptor`) اجرا می‌شود.
- کانتینر PostgreSQL (`postgres:16-alpine`) به ازای هر اجرا یک بار بالا می‌آید
  (`[Collection(nameof(DatabaseCollection))]`) و بعد از هر تست با Respawn (اسکیمای `public`) ریست می‌شود.
- الگوی مشخصه: `[Trait("Category", "Integration")]`؛ نمونه: `Tests/Integration/Order/OrderStatusRepositoryIntegrationTests.cs`.
- بدون Docker، این تست‌ها **Skip** می‌شوند نه Fail (`Xunit.SkippableFact`).

## اجرای تست‌ها

```bash
dotnet test                                     # همه تست‌ها
dotnet test --filter Category=Integration       # فقط تست‌های یکپارچه (نیازمند Docker)
dotnet test --collect:"XPlat Code Coverage"     # پوشش با coverlet
```

پیش‌نیاز خاصی جز Docker (فقط برای Integration) وجود ندارد؛ فایل appsettings یا runner config جداگانه‌ای
برای تست‌ها نیست و پوشش کد آستانه (Threshold) الزام‌آور ندارد.

## نکات و ناهماهنگی‌های فعلی

| مورد | توضیح |
|---|---|
| `ci.yml` نامعتبر | فایل `.github/workflows/ci.yml` در واقع یک باینری XLSX (zip) اشتباهی commit شده و هیچ workflow معتبری وجود ندارد |
| `Tests/TestInfrastructure/Database/` گم‌شده | به دلیل قاعده عام `Database/` در `.gitignore`، فیکسچر کانتینر PostgreSQL در گیت نیست |
| `Tests.csproj.Backup.tmp` | فایل بکاپ commit شده در ریشه Tests |
| `SkippableTestFrameworkSetup.cs` | ارثیه پکیج قدیمی `Xunit.RequiresDockerFact`؛ csproj الان `Xunit.SkippableFact` دارد. اسکریپت ریشه `fix-docker-skip-attributes.ps1` هم `[RequiresDockerFact]`ها را به `[Fact]` بازنویسی می‌کند |
| نبود Fake زمان | فیکه مثل `FakeDateTimeProvider` وجود ندارد؛ تست‌های وابسته به زمان از Builder/Seed استفاده می‌کنند |
