# لایه‌ها و پروژه‌ها

Solution از ۷ پروژه تشکیل شده و هر پروژه مسئولیت، قواعد وابستگی و مرزهای ممنوع مشخصی دارد.
این سند برای هر پروژه مسئولیت، جهت وابستگی مجاز، ساختار پوشه‌ای واقعی و آنتی‌پترن‌های ممنوع را فهرست می‌کند؛
نمای کلان و جهت پیکان‌ها در [overview.md](overview.md) است.

## SharedKernel

**مسئولیت:** پایه‌های مشترک بدون منطق کسب‌وکار؛ تنها پروژه‌ای که هیچ ارجاعی ندارد و همه لایه‌ها به آن وابسته‌اند.

| پوشه | محتوای کلیدی |
|---|---|
| `Abstractions` | `Entity<TId>` (برابری هویتی)، `ValueObject` (برابری مؤلفه‌ای)، `Abstractions/Interfaces` با `IAuditable`، `ISoftDeletable`، `IActivatable`، `IBusinessRule`، `IDateTimeProvider` |
| `Results` | `Error`، `ErrorType`، `ServiceResult`، `ServiceResult<T>`، `ValidationError` |
| `Specifications` | `Specification<T>` با عملگرهای `&` و `|` و `!` و `And/Or/NotSpecification` |
| `Exceptions` | `DomainException` (با `ErrorCode`)، `BusinessRuleViolationException`، `InvalidEntityStateException`، `ExternalServiceException` |
| `Guard` | سه طعم Guard: `Guard`، `ContractGuard` و `DomainGuard` (با پیام فارسی) |
| `ValueObjects` | `IStronglyTypedId` و VOهای عمومی: `Email`، `Money`، `Quantity`، `Percentage`، `Slug`، `FilePath` و ... |
| `Models` | `CurrentUser`، `PaginatedResult<T>` |
| `Localization` | `DomainErrorCodes` با کدهای نقطه‌ای مثل `WALLET.TRANSFER.SELF_NOT_ALLOWED` |
| بقیه | `Attributes` (`SensitiveAttribute`)، `Constants` (`AppRoles`)، `Enums` (`EntityChangeType`)، `Extensions` (`PersianTextNormalizer`)، `Validation` (`IranianIban`)، `Contracts` (خالی) |

**آنتی‌پترن ممنوع:** منطق بیزینسی ماژول‌ها، ارجاع به هر پروژه دیگر، وابستگی به EF Core یا ASP.NET Core.
پکیج‌های فعلی آن فقط abstractionهای Logging و OpenTelemetry هستند.

## SharedContracts

**مسئولیت:** قراردادهای فرا-لایه‌ای که در بیش از یک لایه مصرف می‌شوند؛ بدون هیچ وابستگی پروژه‌ای.

| پوشه | محتوا |
|---|---|
| `Diagnostics` | ActivitySourceهای برنامه (مانند `ApplicationActivitySources`) که Outbox هم برای انتشار trace context استفاده می‌کند |
| `FeatureManagement` | قراردادهای فلگ قابلیت روی `Microsoft.FeatureManagement` |

**آنتی‌پترن ممنوع:** هر نوع پیاده‌سازی، ارجاع به پروژه‌های دیگر یا افزودن DTO خاص ماژول (جای DTOها کنار Featureهای Application است).

## Domain

**مسئولیت:** مدل دامنه غنی؛ برای هر ماژول Aggregate، Entity، ValueObject، Domain Event، Exception و Interface ریپازیتوری.
۲۱ پوشه ماژول دارد (`Attribute`، `Audit`، `Brand`، `Cart`، `Category`، `Discount`، `Inventory`، `Media`، `Notification`،
`Order`، `Payment`، `Product`، `Review`، `Security`، `Shipping`، `Support`، `User`، `Variant`، `Wallet`، `Wishlist` و `Common`)
که ۱۹ تا از آن‌ها پوشه `Aggregates` دارند (`Audit` و `Common` استثنا هستند).

ساختار نمونه (`Domain/Brand`):

```text
Domain/Brand/
├── Aggregates/Brand.cs        # sealed : AggregateRoot<BrandId>, ISoftDeletable
├── Events/                    # BrandCreatedEvent, BrandActivatedEvent, ...
├── Exceptions/                # BrandNameAlreadyExistsException, ...
├── Interfaces/                # IBrandRepository, IBrandUniquenessChecker
└── ValueObjects/              # BrandId, BrandName, BrandSlug
```

الگوهای مدل‌سازی (AggregateRoot، Domain Event، Rule و ...) با مثال واقعی در [domain-modeling.md](domain-modeling.md) آمده است.

**قواعد وابستگی:** فقط `SharedKernel`. تنها پکیج خارجی `BCrypt.Net-Next` است؛ هیچ وابستگی به EF Core، ASP.NET Core یا Redis ندارد.

**آنتی‌پترن ممنوع:** ارجاع به DTO یا Contract لایه Application، فراخوانی Repository پیاده‌شده در Infrastructure،
تزریق `HttpClient` یا سرویس بیرونی در Aggregate، و نگهداری منطق در Handlerهای لایه‌های بالاتر.

## Application

**مسئولیت:** ارکستراسیون استفاده‌های سیستم (Use Caseها) با الگوی CQRS؛ Featureهای Command/Query،
Validation، Behaviors لوله‌ای، قراردادهای سرویس بیرونی و Adapterهای دامنه.
ماژول‌ها هم‌نام با Domain هستند به‌علاوه پوشه‌های پشتیبان: `Analytics`، `Cache`، `Communication`، `Localization`،
`Location`، `Search`، `Storage` و `Common`.

ساختار نمونه یک Feature:

```text
Application/Product/Features/Commands/CreateProduct/
├── CreateProductCommand.cs     # record : ICommand<ProductDetailDto>
├── CreateProductValidator.cs   # AbstractValidator<CreateProductCommand>
└── CreateProductHandler.cs     # sealed : ICommandHandler<...>
```

پوشه `Application/Common` قلب مشترک لایه است: `Behaviors` (۹ رفتار)، `Interfaces`
(`ICommand`، `IQuery`، `IUnitOfWork`، `ICurrentUserService` و ...)، `Events` (`DomainEventNotification<T>`)،
`Contracts`، `Mapping`، `Authorization` و `Services`. الگوی کامل در [cqrs-features.md](cqrs-features.md).

**قواعد وابستگی:** به `Domain`، `SharedContracts` و `SharedKernel`. پکیج‌هایش MediatR، FluentValidation و Mapster
به‌همراه فقط abstractionهای EF Core هستند.

**آنتی‌پترن ممنوع:** ارجاع به `Infrastructure` یا `Presentation`، نوشتن کنترلر یا Middleware،
استفاده از پروایدر Npgsql یا `DbContext` مستقیم، و `SaveChanges` داخل هندلر (مسئولیت `TransactionBehavior` است).

## Infrastructure

**مسئولیت:** همه پیاده‌سازی‌های فنی: Persistence (EF Core/Npgsql)، ریپازیتوری‌ها و QueryServiceها، Outbox،
کش Redis، قفل توزیع‌شده، Elasticsearch، ذخیره S3، پیامک، درگاه پرداخت، Jobهای پس‌زمینه، Health Checkها و DataProtection.
به‌جز `Persistence`، `Common` و پوشه‌های فرا-لایه‌ای (`Chaos`، `DataProtection`، `DistributedLocking`، `BackgroundJobs`)،
ساختار ماژولی ثابتی دارد:

```text
Infrastructure/Product/
├── Configurations/    # IEntityTypeConfiguration<Product>
├── Converters/        # تبدیل StronglyTypedId برای EF
├── Repositories/      # ProductRepository (سمت نوشتن)
└── QueryServices/     # ProductQueryService (سمت خواندن سبک)
```

ثبت خودکار با **Scrutor** انجام می‌شود: کلاس‌هایی که نامشان به `Repository` یا `QueryService` ختم می‌شود
با `AsMatchingInterface` به‌صورت Scoped رجیستر می‌شوند (`Infrastructure/Common/DependencyInjection/InfrastructureServiceExtensions.cs`).
دغدغه‌های مشترک این لایه در [cross-cutting.md](cross-cutting.md) مستند است.

**قواعد وابستگی:** به `Application`، `Domain`، `SharedContracts` و `SharedKernel` — وارونگی وابستگی عمدی برای پیاده‌سازی قراردادهای Application.

**آنتی‌پترن ممنوع:** منطق بیزینسی در Repository/QueryService، ارجاع به `Presentation`،
افزودن Command/Query جدید در این لایه، و اتصال مستقیم به سرویس بیرونی بدون قرارداد در Application.

## Presentation

**مسئولیت:** لبه HTTP: کنترلرهای نسخه‌بندی‌شده، پاکت پاسخ `ApiResponse`، ProblemDetails فارسی، Middlewareها،
Swagger و Composition Root.

```text
Presentation/
├── Program.cs                     # نقطه ورود و ثبت همه سرویس‌ها
├── Base/
│   ├── Endpoints/v1/BaseApiController.cs
│   └── Responses/ApiResponse.cs   # ApiResponse<T>, PaginatedResponse<T>
├── Common/
│   ├── Middleware/  Filters/  ProblemDetails/  Mappers/
│   ├── Extensions/  Options/  Services/  Swagger/
├── <Module>/Endpoints/            # ۵۶ کنترلر، هم‌نام با ماژول
└── appsettings*.json  Dockerfile
```

**قواعد وابستگی:** به `Application`، `Infrastructure` و `SharedContracts` (دسترسی به Domain/SharedKernel فقط transitively).
قراردادهای REST در [05-api/conventions.md](../05-api/conventions.md) مستند است.

**آنتی‌پترن ممنوع:** منطق بیزینسی در کنترلر (تنها نگاشت درخواست به Command/Query)، کار مستقیم با `DbContext`،
دور زدن `HttpResultMapper` برای پاسخ خام، و تعریف Endpoint بدون `[ApiVersion]`.

## Tests

**مسئولیت:** تست واحد همه لایه‌ها و تست یکپارچه ریپازیتوری‌ها روی PostgreSQL واقعی با Testcontainers.
ساختار پوشه‌ها قرینه لایه‌هاست: `Tests/Application`، `Tests/Domain`، `Tests/Infrastructure`، `Tests/Integration`،
`Tests/Presentation`، `Tests/SharedKernel` و زیرساخت مشترک `Tests/TestInfrastructure`
(Assertions، Base، Builders، Fakes، Fixtures، Helpers، Mapping، Stubs، Database).

**قواعد وابستگی:** به هر ۶ پروژه؛ هیچ کد اجرایی به این پروژه وابسته نیست.

**آنتی‌پترن ممنوع:** تست مبتنی بر ترتیب اجرا، اشتراک state بین تست‌ها (پایگاه داده بعد از هر تست با Respawn ریست می‌شود)
و استفاده از EF InMemory (پروژه به‌عمد از آن استفاده نمی‌کند). جزئیات کامل در
[06-testing/testing-strategy.md](../06-testing/testing-strategy.md).
