[بازگشت به فهرست ماژول‌ها](README.md)

# ویژگی‌ها و مقادیر ویژگی (Attributes)

`AttributeType` یک ویژگی کاتالوگ (مثل «رنگ») و `AttributeValue` یکی از مقادیر مجاز آن (مثل «قرمز») است.
واریانت‌ها با `VariantAttribute` به این مقادیر متصل می‌شوند؛ سمت واریانت در [variants.md](variants.md)
مستند شده و این سند فقط تعریف ویژگی‌ها را پوشش می‌دهد.

## دامنه (`Domain/Attribute`)

`AttributeType : AggregateRoot<AttributeTypeId>, IAuditable, IActivatable, ISoftDeletable` با
`Name`, `DisplayName`, `SortOrder`, `IsActive` و مجموعه مقادیر.

| متد | قاعده/اثر |
|---|---|
| `Create` (async) | یکتایی نام از طریق پورت `IAttributeTypeUniquenessChecker`؛ در تکرار `DuplicateAttributeException`؛ صدور `AttributeTypeCreatedEvent` |
| `Update` (async) | بررسی یکتایی فقط هنگام تغییر نام |
| `AddValue` | مقدار تکراری در همان نوع (بدون حساسیت به بزرگی/کوچکی) رد می‌شود؛ صدور `AttributeValueAddedEvent` |
| `UpdateValue` | نبود مقدار → `AttributeValueNotFoundException`؛ تغییر نام با بررسی تکراری |
| `MarkAsDeleted` | حذف نرم + `IsActive = false` |

`AttributeValue : Entity<AttributeValueId>` با `Value`, `DisplayValue`, `HexCode?` (برای نمایش رنگ)،
`SortOrder`, `IsActive`.

**مدل مقادیر، فهرست بسته نیست**: مقدارها رشته آزاد به‌ازای هر نوع‌اند و هیچ لیست از پیش تعیین‌شده‌ای در
کد وجود ندارد؛ `HexCode` فقط برای نمایش رنگ به‌کار می‌رود.

Exceptionها: `DuplicateAttributeException` (`DUPLICATE_ATTRIBUTE`)، `DuplicateAttributeValueException`
(`DUPLICATE_ATTRIBUTE_VALUE`؛ در کد فقط در سطح Handler بررسی می‌شود)، `AttributeTypeNotFoundException`
(`ATTRIBUTE_TYPE_NOT_FOUND`)، `AttributeValueNotFoundException` (`ATTRIBUTE_VALUE_NOT_FOUND`).

Domain Eventها (۲): `AttributeTypeCreatedEvent`, `AttributeValueAddedEvent`.
پوشه‌های `Services` و `Specifications` خالی‌اند و پوشه `Rules` وجود ندارد.

## Application (`Application/Attribute`)

**۶ Command:** `CreateAttributeType`, `UpdateAttributeType`, `DeleteAttributeType`,
`CreateAttributeValue`, `UpdateAttributeValue`, `DeleteAttributeValue` — **هیچ Validator اختصاصی وجود ندارد.**

**۲ Query:** `GetAllAttributeTypes` (صفحه‌بندی‌شده و `ICacheableQuery` با کلید `attributes:all_types`
و انقضای یک ساعت)، `GetAttributeTypeById`.

نکات جریان‌ها:

- ساخت و ویرایش نوع ویژگی از `AttributeTypeUniquenessCheckerAdapter` استفاده می‌کند که
  `IAttributeRepository` را می‌پیچد (الگوی Adapter در
  [02-architecture/cqrs-features.md](../02-architecture/cqrs-features.md)).
- `CreateAttributeValue` پیش از `type.AddValue` تکراری‌بودن مقدار را در Repository می‌سنجد.
- همه Commandها کش `attributes:all_types` را باطل می‌کنند (`AttributeCacheKeys.AllTypes`).

## API

جمع: **۱ کنترلر و ۸ اکشن**؛ همه مدیریتی و بدون کنترلر عمومی.

`AdminAttributesController` — `[Route("api/v{version:apiVersion}/admin/attributes")]` — `[Authorize(Roles = "Admin")]`:

| متد | مسیر | اکشن |
|---|---|---|
| GET | — | `GetAllAttributeTypes` |
| GET | `{id:guid}` | `GetAttributeType` |
| POST | — | `CreateAttributeType` |
| POST | `{typeId:guid}/values` | `CreateAttributeValue` |
| PUT | `{id:guid}` | `UpdateAttributeType` |
| PUT | `values/{id:guid}` | `UpdateAttributeValue` |
| DELETE | `{id:guid}` | `DeleteAttributeType` |
| DELETE | `values/{id:guid}` | `DeleteAttributeValue` |

قواعد مسیر و پاکت پاسخ در [05-api/conventions.md](../05-api/conventions.md).

## زیرساخت

- `AttributeTypeConfiguration` و `AttributeValueConfiguration` از Interceptor نسخه‌گذاری سطر
  (`AddInterceptorRowVersion`) استفاده می‌کنند و فیلتر سراسری `!e.IsDeleted` دارند — این یکی از
  معدود بخش‌هایی است که حذف نرم را با Query Filter در EF اعمال می‌کند.
- طول `Name`/`DisplayName`/`Value` صد کاراکتر و `HexCode` پنجاه کاراکتر است؛ حذف `Values` آبشاری است.
- کش سمت خواندن با `ICacheableQuery` و ابطال دستی در Commandها انجام می‌شود؛ جزئیات سازوکار در
  [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md).

## نکات و محدودیت‌ها

- `GetAttributeTypeByIdQuery` در کد به‌صورت `ICommand<AttributeTypeDto>` اعلام شده است، نه `IQuery`؛
  یعنی از نظر پلی‌پ‌لاین مانند Command رفتار می‌کند (بدون Cache) هرچند سمت خواندن است.
- پکیج ویژگی‌ها Validator ندارد؛ اعتبارسنجی عمدتاً در Domain و از طریق بررسی یکتایی انجام می‌شود.
- مرز ماژول: اتصال ویژگی به واریانت (`VariantAttribute`) در [variants.md](variants.md) توضیح داده شده؛
  این ماژول هیچ اشاره‌ای به محصول یا واریانت ندارد.

## اسناد مرتبط

- واریانت و ترکیب ویژگی‌ها: [variants.md](variants.md)
- ساختار Feature، Adapter و QueryService: [02-architecture/cqrs-features.md](../02-architecture/cqrs-features.md)
