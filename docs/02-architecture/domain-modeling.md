# الگوهای مدل‌سازی دامنه

لایه Domain با بلوک‌های سازنده استاندارد DDD ساخته شده: AggregateRoot، Entity، ValueObject،
Domain Event، Specification، Rule و Domain Exception. این سند این بلوک‌ها را با مثال واقعی
Aggregate برند و محصول مستند می‌کند و نشان می‌دهد رویدادها چطور تولید و جمع‌آوری می‌شوند.

## بلوک‌های سازنده

| بلوک | کلاس پایه / قرارداد | مسیر | نقش |
|---|---|---|---|
| Aggregate | `AggregateRoot<TId> : Entity<TId>` | `Domain/Common/Abstractions/AggregateRoot.cs` | نگهداری لیست خصوصی رویدادها، `Version` و `RaiseDomainEvent` (هر صدور، یک واحد افزایش) |
| Entity | `Entity<TId>` | `SharedKernel/Abstractions/Entity.cs` | برابری هویتی با عملگرهای `==`/`!=` |
| ValueObject | `ValueObject` | `SharedKernel/Abstractions/ValueObject.cs` | برابری مؤلفه‌ای با `GetEqualityComponents()` |
| StronglyTypedId | `IStronglyTypedId` | `SharedKernel/ValueObjects/` | شناسه‌های نوع‌دار مثل `BrandId`، `ProductId` با متد `NewId()` و تبدیل ضمنی به Guid |
| قابلیت‌های موجودیت | `IAuditable`، `ISoftDeletable`، `IActivatable`، `IBusinessRule` | `SharedKernel/Abstractions/Interfaces/` | پرچم‌هایی که Interceptorها و رفتارهای پلیپ‌لاین به آن‌ها واکنش نشان می‌دهند |
| Domain Event | `IDomainEvent` و کلاس انتزاعی `DomainEvent` | `Domain/Common/Abstractions/IDomainEvent.cs` و `Domain/Common/Events/DomainEvent.cs` | `EventId`، `OccurredAt`، `CorrelationId`، `CausationId` و `EventVersion` |
| Specification | `Specification<T>` | `SharedKernel/Specifications/` | ترکیب‌پذیر با عملگرهای `&`، `|` و `!` و کامپایل کش‌شده Expression |
| Rule | `IBusinessRule` | `SharedKernel/Abstractions/Interfaces/IBusinessRule.cs` | `IsBroken()` و `Message`؛ نقض آن با `BusinessRuleViolationException` |
| Exception | `DomainException` | `SharedKernel/Exceptions/DomainException.cs` | پایه با `ErrorCode` و پیام فارسی؛ ۴۸ کلاس استثنا در ماژول‌های Domain |

سه نکته وضعیت فعلی کد:

- پوشه‌های `Domain/Product/Specifications` و `Domain/Product/Rules` وجود دارند اما **خالی‌اند**؛
  تنها Ruleهای فعال `Domain/Wallet/FraudDetection/Rules/` هستند (`HighVelocityRule`، `MultipleFailedTopUpRule`،
  `RapidTopUpWithdrawRule`، `UnusualAmountRule` — هر یک `IFraudDetectionRule` با متد `EvaluateAsync`).
- `Result`/`ServiceResult` در دامنه استفاده نمی‌شود؛ دامنه با استثنا صحبت می‌کند و ترجمه به
  `ServiceResult` در Behavior پلیپ‌لاین انجام می‌شود (see [cqrs-features.md](cqrs-features.md)).
- Guard سه طعم دارد: `Guard` و `ContractGuard` (استثنای استاندارد) و `DomainGuard` (استثنای دامنه با پیام فارسی) — `SharedKernel/Guard/`.

## مثال واقعی: Aggregate برند

`Domain/Brand/Aggregates/Brand.cs` — `sealed class Brand : AggregateRoot<BrandId>, ISoftDeletable`
نمونه کامل الگوهاست:

| الگو | پیاده‌سازی در برند |
|---|---|
| سازنده خصوصی | دو سازنده private: بدون‌پارامتر برای EF و سازنده کامل state که در پایان `RaiseDomainEvent(new BrandCreatedEvent(...))` را صدا می‌زند |
| فکتوری Async | `static Task<Brand> Create(BrandName, BrandSlug, CategoryId, IBrandUniquenessChecker, ...)` — سرویس دامنه یکتایی را چک می‌کند و تکرار یعنی `BrandNameAlreadyExistsException` |
| کپسوله‌سازی مجموعه | `private readonly List<ProductId> _products` با خروجی `IReadOnlyCollection<ProductId> Products` |
| گذارهای محافظت‌شده | `Activate()` در حالت فعال `BrandAlreadyActiveException` می‌دهد؛ `Deactivate()` مشابه؛ `UpdateDetails` دوباره یکتایی چک می‌کند؛ تغییری که واقعاً تغییری نباشد no-op است |
| رویداد به‌ازای گذار | `BrandCreatedEvent`، `BrandUpdatedEvent`، `BrandCategoryChangedEvent`، `BrandActivatedEvent`، `BrandDeactivatedEvent`، `BrandDeletedEvent` (`Domain/Brand/Events/`) |
| حذف نرم | `RequestDeletion()` مقدار `IsDeleted/DeletedAt/DeletedBy` را پر و برند را غیرفعال می‌کند |
| ValueObject اختصاصی | `BrandId`، `BrandName` (فکتوری `Create` با محدوده ۲ تا ۱۰۰ کاراکتر و پیام فارسی)، `BrandSlug` (`Domain/Brand/ValueObjects/`) |

نمونه Aggregate محصول (`Domain/Product/Aggregates/Product.cs`) همین الگو را تکرار می‌کند با این تفاوت‌ها:

- به‌جای ساختن VO از نو، به VOهای ماژول دیگر ارجاع دارد (`BrandId`، `CategoryId`).
- مجموعه تنوع‌ها به Entity ماژول Variant اشاره می‌کند: `private List<ProductVariant> _variants`
  (نه مالکیت فایل‌محور؛ `ProductVariant` در `Domain/Variant/Aggregates/ProductVariant.cs` است).
- رویدادها: `ProductCreatedEvent`، `ProductUpdatedEvent`، `ProductBrandChangedEvent`،
  `ProductCategoryChangedEvent`، `ProductActivatedEvent`، `ProductDeactivatedEvent` و `PriceChangedEvent`
  (که از سمت Variant صادر می‌شود).
- چند متد فقط state را عوض می‌کنند و رویداد نمی‌دهند (`Restore`، `MarkAsFeatured`، `UnmarkAsFeatured`)
  و `RecalculateReviewStats` ناورستی‌ها را با `DomainException` پیام فارسی کنترل می‌کند.

## Exceptionهای دامنه

استثناها در `Domain/<Module>/Exceptions/` زندگی می‌کنند و از `DomainException` ارث می‌برند؛
کلاس پایه `ErrorCode` (پیش‌فرض `DOMAIN_ERROR`) و دیکشنری `Args` دارد. نمونه‌ها:

| ماژول | استثناها |
|---|---|
| Security | `InvalidOtpCodeException`، `OtpExpiredException`، `OtpMaxAttemptsExceededException`، `SessionExpiredException` |
| Order | `EmptyOrderException`، `InvalidOrderTransitionException`، `OrderAlreadyPaidException`، `OrderCancellationNotAllowedException` |
| Wallet | `InsufficientWalletBalanceException`، `InvalidTopUpAmountException`، `WalletTransferLimitExceededException` |
| بقیه | `InvalidCartQuantityException`، `DiscountCodeNotRedeemableException`، `PaymentExpiredException`، `DuplicateCategoryNameException`، `InvalidFileTypeException` و ... |

ترجمه این استثناها به پاسخ HTTP در دو نقطه رخ می‌دهد: Behavior پلیپ‌لاین (`DomainExceptionBehavior`) و
Middleware (`CustomExceptionHandlerMiddleware` با `PersianProblemDetailsFactory`) — see [05-api/conventions.md](../05-api/conventions.md).

## جمع‌آوری رویدادها و مسیر Outbox

`RaiseDomainEvent` رویداد را به لیست خصوصی Aggregate اضافه و `Version` را زیاد می‌کند؛
Interceptor دیتابیس (`Infrastructure/Persistence/Interceptors/DomainEventInterceptor.cs`) هنگام
`SaveChanges` رویدادها را به ردیف‌های `OutboxMessage` تبدیل می‌کند و پردازش نامتقارن با
`OutboxProcessor` انجام می‌شود. جریان کامل، retry و نکات عملیاتی در [cross-cutting.md](cross-cutting.md) است.

> **نکته wiring در کد فعلی:** `DomainEventInterceptor` موجودیت‌ها را با شرط `IHasDomainEvents`
> جست‌وجو می‌کند، اما `AggregateRoot<TId>` این Interface را implement نکرده است و در کل ریپو
> پیاده‌سازی دیگری هم برای آن یافت نشد؛ نتیجه اینکه رویدادهای صادرشده با `RaiseDomainEvent`
> در مسیر فعلی Interceptor به Outbox نوشته نمی‌شوند. اتصال مورد انتظار، پیاده‌سازی
> `IHasDomainEvents` توسط `AggregateRoot<TId>` است.
