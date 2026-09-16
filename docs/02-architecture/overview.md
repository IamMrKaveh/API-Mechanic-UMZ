# نمای کلی معماری

معماری ریپو یک Clean Architecture کلاسیک چهار لایه به‌همراه دو پروژه مشترک (`SharedKernel` و `SharedContracts`) است.
این سند قاعده وابستگی، دیاگرام لایه‌ها، جریان یک درخواست از Endpoint تا دیتابیس و جایگاه Outbox و Domain Event
در یک نگاه را پوشش می‌دهد؛ جزئیات هر موضوع به اسناد اختصاصی بخش ۰۲ ارجاع داده می‌شود.

## قاعده وابستگی

جهت وابستگی همیشه به سمت هسته است: `Presentation` به `Infrastructure`، `Infrastructure` به `Application`،
`Application` به `Domain` و `Domain` فقط به `SharedKernel`. نکته کلیدی، **وارونگی وابستگی** در دو نقطه است:

- `Infrastructure` به `Application` ارجاع می‌دهد تا قراردادهای تعریف‌شده در Application (مثل `IUnitOfWork`،
  `ICacheService` و QueryServiceها) را پیاده کند.
- قراردادهای ریپازیتوری و سرویس‌های دامنه (مثل `IBrandRepository` و `IBrandUniquenessChecker`) در خود
  `Domain/<Module>/Interfaces` تعریف می‌شوند و در `Application` (به‌صورت Adapter) و `Infrastructure` (به‌صورت EF) پیاده می‌شوند.

```text
                 ┌────────────────────────────────────────────┐
                 │              Tests (همه لایه‌ها)            │
                 └────────────────────────────────────────────┘
                                    │
  ┌─────────────┐            ┌─────────────┐
  │Presentation │───────────►│Infrastructure│
  │ (Composition│            └──────┬───────┘
  │   Root)     │                   │
  └──────┬──────┘                   ▼
         │                   ┌─────────────┐
         │                   │ Application  │────────────┐
         │                   └──────┬───────┘            │
         │                          ▼                    │
         │                   ┌─────────────┐            │
         └──────────────────►│    Domain    │────────────┤
                             └──────┬───────┘            │
                                    ▼                    ▼
                             ┌─────────────┐   ┌─────────────────┐
                             │ SharedKernel │   │  SharedContracts │
                             └─────────────┘   └─────────────────┘
```

| پروژه | ارجاع به | ارجاع از |
|---|---|---|
| `SharedKernel` | هیچ | Domain، Application، Infrastructure، Tests |
| `SharedContracts` | هیچ | Application، Infrastructure، Presentation، Tests |
| `Domain` | SharedKernel | Application، Infrastructure، Tests |
| `Application` | Domain، SharedContracts، SharedKernel | Infrastructure، Presentation، Tests |
| `Infrastructure` | Application، Domain، SharedContracts، SharedKernel | Presentation، Tests |
| `Presentation` | Application، Infrastructure، SharedContracts | Tests |
| `Tests` | هر ۶ پروژه | — |

مسئولیت و آنتی‌پترن‌های ممنوع هر پروژه در [layers.md](layers.md) آمده است.

## جریان یک درخواست

مسیر یک Command نمونه (مثلاً `POST api/v1/admin/products`) از بالا تا پایین:

| گام | لایه / کامپوننت | اتفاق |
|---|---|---|
| ۱ | Middleware pipeline | زنجیره Middlewareهای `Presentation/Common/Middleware` (CorrelationId، Exception Handler، Rate Limit، Auth و ...) — فهرست کامل و ترتیب در [05-api/conventions.md](../05-api/conventions.md) |
| ۲ | Controller | کلاسی مانند `AdminProductsController` (ارث‌بری از `BaseApiController`) درخواست را به `CreateProductCommand` نگاشت و با `Mediator.Send` ارسال می‌کند |
| ۳ | Pipeline Behaviors | نه رفتار لوله‌ای `Application/Common/Behaviors` به‌ترتیب اجرا می‌شوند: لاگ، Validation، ترجمه DomainException، کش، Idempotency، تراکنش، حسابرسی — جزئیات در [cqrs-features.md](cqrs-features.md) |
| ۴ | Handler | `CreateProductHandler` از طریق Repository Interface دامنه Aggregate را می‌سازد (`Product.Create` → صدور `ProductCreatedEvent`) و ذخیره می‌کند؛ هندلر خودش `SaveChanges` نمی‌زند |
| ۵ | TransactionBehavior | پس از هندلر، `IUnitOfWork.SaveChangesAsync` (پیاده‌سازی `UnitOfWork` در `Infrastructure/Persistence`) در Execution Strategy فراخوانی می‌شود |
| ۶ | Interceptors | `AuditableEntityInterceptor` زمان‌های حسابرسی و `DomainEventInterceptor` رویدادهای دامنه را در транزیکشن ثبت می‌کند |
| ۷ | Outbox | رویدادها به‌صورت `OutboxMessage` در همان تراکنش ذخیره و سپس به‌صورت پس‌زمینه‌ای پردازش و با MediatR منتشر می‌شوند — جزئیات در [cross-cutting.md](cross-cutting.md) |
| ۸ | پاسخ | `ServiceResult<T>` هندلر با `HttpResultMapper` به پاکت `ApiResponse` و کد HTTP مناسب تبدیل می‌شود |

## Domain Event و Outbox در یک نگاه

- Aggregateها با `RaiseDomainEvent` رویداد صادر می‌کنند و این کار `Version` را افزایش می‌دهد
  (`Domain/Common/Abstractions/AggregateRoot.cs`).
- هنگام `SaveChanges`، `DomainEventInterceptor` رویدادها را به ردیف‌های `OutboxMessage` (JSON با trace context) تبدیل می‌کند؛
  یعنی انتشار رویداد با ذخیره‌سازی داده اتمیک است.
- `OutboxProcessingJob` هر ۱۵ ثانیه با قفل Redis پیام‌های پردازش‌نشده را برمی‌دارد، آن‌ها را در
  `DomainEventNotification<T>` می‌پیچد و با `IPublisher` منتشر می‌کند؛ مصرف‌کننده‌ها (مثل
  `ProductCacheInvalidationHandler` برای ابطال کش یا سرویس‌های Elasticsearch) واکنش نشان می‌دهند.
- جریان کامل، جدول retry و نکته wiring مربوط به `IHasDomainEvents` در [cross-cutting.md](cross-cutting.md) است.

## Composition Root

ثبت سرویس‌ها فقط در `Presentation/Program.cs` رخ می‌دهد و ترتیب آن:

1. `AddApplicationAuthentication` (JWT)
2. `AddPresentation` (کنترلرها، نسخه‌بندی، Options، CORS، Observability، Localization، Rate Limiting، Chaos)
3. `AddApplicationServices` (مدیاتور، Validatorها، Mappingها — `Application/Common/DependencyInjection/ApplicationServiceCollection.cs`)
4. ثبت `WalletTransferOptions` و `ReviewSettings`
5. `AddInfrastructure` (دیتابیس، Redis، Elasticsearch، Jobها، Health Checkها — `Infrastructure/Common/DependencyInjection/InfrastructureServiceExtensions.cs`)
6. `ValidateRequiredConfiguration` (اجبار تنظیمات حیاتی؛ فهرست در [01-getting-started/configuration.md](../01-getting-started/configuration.md))

## ماژول‌های عمودی

هر ماژول دامنه یک برش عمودی کامل دارد: پوشه هم‌نام در هر چهار لایه
(`Domain/Product`، `Application/Product`، `Infrastructure/Product`، `Presentation/Product`).
در سطح Infrastructure این ساختار ثابت است: `Configurations/` (EF)، `Converters/` (تبدیل StronglyTypedId)،
`Repositories/` (سمت نوشتن) و `QueryServices/` (سمت خواندن).
