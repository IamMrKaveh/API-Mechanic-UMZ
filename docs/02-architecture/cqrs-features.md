# الگوی CQRS و Featureها

لایه Application با Featureهای عمودی CQRS کار می‌کند: هر عملیات یک پوشه با Command/Query،
Validator و Handler دارد و Behaviors مشترک، نگرانی‌های فرا-برشی را دور هندلر می‌پیچند.
این سند آناتومی یک Feature واقعی، پلیپ‌لاین Behaviors، الگوهای Contract/Mapping/Adapter و
الگوی QueryService سمت Infrastructure را با نام‌های واقعی کد مستند می‌کند.

## آناتومی یک Feature (مثال واقعی CreateProduct)

| جزء | نام کلاس | مسیر |
|---|---|---|
| Command | `CreateProductCommand` — record با `ICommand<ProductDetailDto>` و `IAuditableCommand` | `Application/Product/Features/Commands/CreateProduct/CreateProductCommand.cs` |
| Validator | `CreateProductValidator` — `AbstractValidator<CreateProductCommand>` با پیام فارسی | `Application/Product/Features/Commands/CreateProduct/CreateProductValidator.cs` |
| Handler | `CreateProductHandler` — sealed، تزریق با primary constructor، پیاده‌سازی `ICommandHandler<CreateProductCommand, ProductDetailDto>` | `Application/Product/Features/Commands/CreateProduct/CreateProductHandler.cs` |
| DTO پاسخ | `ProductDetailDto` — record مشترک Featureها | `Application/Product/Features/Shared/ProductDtos.cs` |
| Mapping | `ProductMappingConfig` — `IRegister` مپستر از Aggregate به DTO | `Application/Product/Mapping/ProductMappingConfig.cs` |
| Endpoint | `AdminProductsController` با `[Route("api/v{version:apiVersion}/admin/products")]` و `[Authorize(Roles = "Admin")]` | `Presentation/Product/Endpoints/AdminProductsController.cs` |

جریان `CreateProductHandler` گام‌به‌گام:

1. تبدیل Guid به ValueObject دامنه: `CategoryId.From(...)` و `BrandId.From(...)`.
2. واکشی دسته و برند از ریپازیتوری؛ نبودِ آن‌ها یعنی `ServiceResult.NotFound(...)`.
3. تولید Slug با `ProductSlug.GenerateFrom(request.Name)` و کنترل یکتایی با `ExistsBySlugAsync`؛ تکرار یعنی `Conflict`.
4. ساخت Aggregate با فکتوری دامنه: `Product.Create(ProductName.Create(name), slug, ...)`
   (رویداد `ProductCreatedEvent` همین‌جا در Aggregate صادر می‌شود، نه در هندلر).
5. ذخیره با `productRepository.AddAsync(product, ct)` — هندلر `SaveChanges` نمی‌زند؛ این کار
   `TransactionBehavior` بعد از `next()` انجام می‌دهد.
6. نگاشت به DTO با Mapster و تکمیل فیلدهای نمایشی با record `with`.
7. بازگشت `ServiceResult<ProductDetailDto>.Success(dto)`.

قراردادهای پایه همه در `Application/Common/Interfaces` تعریف شده‌اند:

| قرارداد | نقش |
|---|---|
| `ICommand` / `ICommand<T>` و `IQuery` / `IQuery<T>` | برچسب Command/Query روی `IRequest` مدیاتور |
| `ICommandHandler<T,TResult>` / `IQueryHandler<T,TResult>` | هندلرهای استاندارد که همگی `ServiceResult<TResult>` برمی‌گردانند |
| `IUnitOfWork` | فقط `SaveChangesAsync` و `ExecuteStrategyAsync<T>`؛ پیاده‌سازی `UnitOfWork` در `Infrastructure/Persistence/UnitOfWork.cs` |
| `ITransaction` | مارکر تراکنش (`IDisposable, IAsyncDisposable`) |
| `ICacheableQuery` | کلید کش و expiry برای رفتار کش |
| `IAuditableCommand` | متادیتای حسابرسی برای رفتار AuditingBehavior |

## پلیپ‌لاین Behaviors

هر ۹ رفتار در `Application/Common/Behaviors` تعریف شده و در
`Application/Common/DependencyInjection/ApplicationServiceCollection.cs` (متد `RegisterMediatR`)
دقیقاً به همین ترتیب ثبت می‌شوند:

| ترتیب | رفتار | نقش |
|---|---|---|
| ۱ | `UnhandledExceptionLoggingBehavior<,>` | لاگ آخرین مرتبه استثناهای فراری از پلیپ‌لاین |
| ۲ | `LoggingBehavior<,>` | لاگ ساختاریافته شروع/پایان هر درخواست |
| ۳ | `QueryLoggingBehavior<,>` | لاگ هدفمند برای Queryها |
| ۴ | `ValidationBehavior<,>` | اجرای Validatorهای FluentValidation و قطعه‌کوتاه با خطای Validation |
| ۵ | `DomainExceptionBehavior<,>` | ترجمه `DomainException` به `ServiceResult` |
| ۶ | `CachingBehavior<,>` | کش خواندنی برای `ICacheableQuery` از طریق `ICacheService` |
| ۷ | `IdempotencyBehavior<,>` | بازگرداندن نتیجه قبلی برای Commandهای دارای `IdempotencyKey` (با قفل توزیع‌شده اختیاری) |
| ۸ | `TransactionBehavior<,>` | Execution Strategy + `IUnitOfWork.SaveChangesAsync` برای Commandها؛ رد شدن برای Query و `IBypassTransactionBehavior`/`IManualTransactionRequest`؛ نگاشت `DbUpdateException` یکتایی/کلید خارجی به Conflict |
| ۹ | `AuditingBehavior<,>` | ثبت رکورد حسابرسی برای `IAuditableCommand` با غنی‌سازی کاربر/IP |

## الگوهای Contract و Mapping و Adapter

سه الگوی مکمل برای عبور درست وابستگی بین لایه‌ها:

| الگو | جای تعریف | مثال واقعی |
|---|---|---|
| Contract | `Application/<Module>/Contracts/` — قرارداد سرویس‌های خواندن/بیرونی که همه‌جا با global using های `Application/Imports.cs` دیده می‌شوند | `IProductQueryService`، `ICacheService`، `IJwtTokenGenerator` |
| Mapping | `Application/<Module>/Mapping/*MappingConfig.cs` — `IRegister` مپستر که با `AddApplicationMappings` اسکن و Compile می‌شود | `ProductMappingConfig`، `ProductQueryMappingConfig` |
| Adapter | `Application/<Module>/Adapters/` — کلاس sealed که یک Interface **دامنه** را با سرویس‌های Application پیاده می‌کند | `BrandUniquenessCheckerAdapter : IBrandUniquenessChecker` (Interface از `Domain/Brand/Interfaces`) و `PaymentInitiator : IPaymentInitiator` (پل دامنه به MediatR) |

Adapterها نقطه وارونگی وابستگی دوم هستند: دامنه بدون شناخت Application/Infrastructure،
خدماتی مثل «بررسی یکتایی برند» را درخواست می‌کند.

## سمت Query و الگوی QueryService

خواندن‌ها دو مسیر دارند:

1. **Queryهای MediatR** برای واکشی تکی/سبک: رکوردی مثل `GetProductQuery(Guid Id) : IQuery<ProductDetailDto>, ICacheableQuery`
   با `CacheKey => CacheKeys.Product(Id)` و هندلر `GetProductHandler` در همان پوشه
   (`Application/Product/Features/Queries/GetProduct/`).
2. **QueryService در Infrastructure** برای لیست‌ها، پنل ادمین و پروجکشن‌های سنگین: قرارداد در Application
   (`Application/Product/Contracts/IProductQueryService.cs`) و پیاده‌سازی `ProductQueryService` در
   `Infrastructure/Product/QueryServices/ProductQueryService.cs` با `AsNoTracking` و
   `AsSplitQuery` و نگاشت مستقیم به DTO — بدون عبور از Aggregate. همین الگو برای
   `AnalyticsQueryService`، `UserQueryService`، `StockLedgerQueryService` و بقیه تکرار شده است.

## مقیاس فعلی

شمارش فایل‌سیستمی (متد: شمارش `*Handler.cs` در پوشه‌های `Features/Commands` و `Features/Queries`،
تقاطع‌سنجی با grep از `: ICommandHandler` (۱۶۱ مورد) و `: IQueryHandler` (۱۲۱ مورد)):

| سمت | تعداد فایل هندلر |
|---|---|
| Commands | ۱۶۵ |
| Queries | ۱۲۶ |

بزرگ‌ترین ماژول‌ها به ترتیب: Wallet (۲۲/۱۷)، Order (۱۸/۷)، Review (۱۷/۷)، Inventory (۱۴/۱۳)، User (۱۳/۶).
ماژول‌های `Cache`، `Communication`، `Localization`، `Security` و `Storage` پوشه `Features` ندارند
و فقط قرارداد/سرویس پشتیبان‌اند.

## ثبت DI

`AddApplicationServices` در `Application/Common/DependencyInjection/ApplicationServiceCollection.cs`
پنج کار می‌کند: `RegisterMediatR` (اسکن اسمبلی + ثبت ۹ Behavior)، `RegisterDomainServices`،
`RegisterApplicationServices`، `RegisterValidators` (اسکن اسمبلی FluentValidation) و
`AddApplicationMappings` (اسکن `IRegister`های مپستر به‌صورت Singleton).
