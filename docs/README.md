# مستندات Mechanic API

این پوشه مرجع رسمی مستندات پروژه است. همه اسناد به زبان فارسی و بر پایه کد واقعی مخزن نوشته شده‌اند؛
نام کلاس‌ها، مسیرها و اصطلاحات فنی به همان شکل انگلیسی کد باقی مانده‌اند.
سند حاضر فهرست اصلی، درخت کامل اسناد و نقشه راه فازهای بعدی داکیومنت‌سازی است.

## درباره پروژه

Mechanic API یک API فروشگاهی اینترنتی روی .NET 9 است که با معماری Clean و الگوی CQRS سازمان‌دهی شده است.
دامنه در ۲۱ ماژول مستقل (کاتالوگ، سفارش، پرداخت، کیف پول، انبار، تخفیف، نظر و ...) تقسیم شده و هر ماژول به‌صورت عمودی در
لایه‌های `Domain`، `Application`، `Infrastructure` و `Presentation` امتداد یافته است. ذخیره‌سازی روی PostgreSQL با EF Core،
کش و قفل توزیع‌شده روی Redis، جست‌وجو روی Elasticsearch و پیام‌رسانی داخلی بر پایه Outbox و MediatR است.

| مشخصه | مقدار |
|---|---|
| Solution | `API.sln` با ۷ پروژه |
| نسخه SDK | `9.0.314` (طبق `global.json`) |
| دیتابیس | PostgreSQL / Npgsql (`Infrastructure/Persistence/Context/DBContext.cs`) |
| الگوی درخواست | CQRS با MediatR ۱۴ و FluentValidation |
| نقطه ورود اجرا | `Presentation/Program.cs` |
| نام‌های به‌کاررفته در کد | عنوان Swagger: «Ledka API» — نام برنامه در لاگ Serilog: «Mechanic.Api» |

## نقشه اسناد موجود

| سند | موضوع |
|---|---|
| [01-getting-started/introduction.md](01-getting-started/introduction.md) | معرفی پروژه، فهرست تکنولوژی‌ها، نمای کلی Solution |
| [01-getting-started/setup-and-run.md](01-getting-started/setup-and-run.md) | پیش‌نیازها، اجرای API، اعمال مایگریشن‌ها، وضعیت docker-compose |
| [01-getting-started/configuration.md](01-getting-started/configuration.md) | ساختار appsettings، کلاس‌های Options، متغیرهای محیطی |
| [02-architecture/overview.md](02-architecture/overview.md) | نمای کلان Clean Architecture و جریان یک درخواست |
| [02-architecture/layers.md](02-architecture/layers.md) | مسئولیت و قواعد وابستگی هر ۷ پروژه + آنتی‌پترن‌ها |
| [02-architecture/cqrs-features.md](02-architecture/cqrs-features.md) | ساختار Feature، Behaviors، Contract/Mapping/Adapter، QueryService |
| [02-architecture/domain-modeling.md](02-architecture/domain-modeling.md) | Aggregate، Entity، ValueObject، Domain Event، Rule، Exception |
| [02-architecture/cross-cutting.md](02-architecture/cross-cutting.md) | Outbox، Interceptorها، قفل توزیع‌شده، Jobها، Health Check، Chaos |
| [03-modules/README.md](03-modules/README.md) | فهرست ۲۲ سند ماژول‌های بیزینسی (کاتالوگ، فروش، کیف پول، اعلان و ...) |
| [04-integrations/README.md](04-integrations/README.md) | فهرست اسناد یکپارچه‌ها و سرویس‌های زیرساختی |
| [04-integrations/payment-gateways.md](04-integrations/payment-gateways.md) | لایه فنی درگاه زرین‌پال، Factory، Mock، nonce و Health Check |
| [04-integrations/elasticsearch.md](04-integrations/elasticsearch.md) | لایه فنی Elasticsearch: DI، ایندکس، Outbox اختصاصی، کلیدشکن |
| [04-integrations/cache-redis.md](04-integrations/cache-redis.md) | Redis، کش، رمزنگاری محتوا، قفل توزیع‌شده، idempotency، ابطال رویدادی |
| [04-integrations/background-jobs.md](04-integrations/background-jobs.md) | فهرست کامل Jobهای پس‌زمینه و Seederها با دوره و قفل هر یک |
| [04-integrations/location.md](04-integrations/location.md) | استان و شهر از API بیرونی مکان |
| [04-integrations/localization.md](04-integrations/localization.md) | فرهنگ‌ها، رشته‌های خطا و کدهای خطای دامنه |
| [05-api/conventions.md](05-api/conventions.md) | قراردادهای سراسری REST: مسیردهی، نسخه‌بندی، پاکت پاسخ، خطا، Swagger |
| [06-testing/testing-strategy.md](06-testing/testing-strategy.md) | ساختار پروژه Tests، TestInfrastructure، اجرای تست‌ها |
| [07-operations/deployment.md](07-operations/deployment.md) | Dockerfile، تحلیل docker-compose، مایگریشن و نکات انتشار |
| [07-operations/security.md](07-operations/security.md) | DataProtection، ماژول Security، gitleaks و pre-commit |
| [08-appendix/glossary.md](08-appendix/glossary.md) | واژه‌نامه اصطلاحات با ارجاع به کد واقعی و سند اصلی |

## درخت کامل مستندات

```text
docs/
├── README.md
├── 01-getting-started/
│   ├── introduction.md
│   ├── setup-and-run.md
│   └── configuration.md
├── 02-architecture/
│   ├── overview.md
│   ├── layers.md
│   ├── cqrs-features.md
│   ├── domain-modeling.md
│   └── cross-cutting.md
├── 03-modules/            ← اسناد ماژول‌های بیزینسی (فاز ۲ — تکمیل‌شده، ۲۲ سند + فهرست)
├── 04-integrations/       ← یکپارچه‌ها و سرویس‌های زیرساختی (فاز ۳ — ۶ سند + فهرست)
│   ├── payment-gateways.md
│   ├── elasticsearch.md
│   ├── cache-redis.md
│   ├── background-jobs.md
│   ├── location.md
│   └── localization.md
├── 05-api/
│   └── conventions.md
├── 06-testing/
│   └── testing-strategy.md
├── 07-operations/
│   ├── deployment.md
│   └── security.md
└── 08-appendix/
    └── glossary.md
```

شماره‌گذاری پوشه‌ها با فاصله (۰۱، ۰۲، ۰۳، ۰۵، ۰۶، ۰۷) انجام شده تا افزودن بخش‌های تازه در فازهای بعدی
نیازمند جابه‌جایی یا تغییر نام اسناد موجود نباشد.

## وضعیت فازهای داکیومنت‌سازی

| فاز | دامنه | وضعیت |
|---|---|---|
| ۱ | شروع، معماری، قراردادهای API، تست و عملیات (پوشه‌های ۰۱، ۰۲، ۰۵، ۰۶، ۰۷) | تکمیل‌شده |
| ۲ | ماژول‌های بیزینسی — ۲۲ سند در `03-modules/` | تکمیل‌شده — فهرست کامل در [03-modules/README.md](03-modules/README.md) |
| ۳ | یکپارچه‌ها و سرویس‌های زیرساختی در `04-integrations/` و واژه‌نامه در `08-appendix/` | تکمیل‌شده — همین نسخه |
| ۴ | در این نقشه هنوز تعریف نشده است | — |

مرزبندی موضوعات پابرجاست: هر سند ماژول فقط جزئیات همان ماژول (Aggregateها، Endpointها،
Command/Queryها و قواعد بیزینسی) را دارد؛ قراردادهای سراسری در
[05-api/conventions.md](05-api/conventions.md)، الگوهای مشترک در بخش ۰۲ و لایه فنی سرویس‌های بیرونی
در `04-integrations/` باقی می‌مانند.

## ترتیب پیشنهادی مطالعه

| ترتیب | برای چه کسی | مسیر مطالعه |
|---|---|---|
| ۱ | راه‌اندازی سریع | `introduction.md` → `setup-and-run.md` |
| ۲ | فهم معماری | `overview.md` → `layers.md` → `cqrs-features.md` |
| ۳ | توسعه یک قابلیت | `cqrs-features.md` → `domain-modeling.md` → `05-api/conventions.md` |
| ۴ | کار عملیاتی | `configuration.md` → `07-operations/deployment.md` → `07-operations/security.md` |
| ۵ | زیرساخت و یکپارچه‌ها | `cross-cutting.md` → `04-integrations/cache-redis.md` → `04-integrations/background-jobs.md` → `04-integrations/payment-gateways.md` → `04-integrations/elasticsearch.md` → `08-appendix/glossary.md` |

## قواعد نگارش اسناد

| قاعده | توضیح |
|---|---|
| زبان | فارسی روان؛ نام کلاس، پوشه، مسیر فایل و مسیر API به انگلیسی و داخل `inline code` |
| ساختار | هر سند با یک H1 و سپس خلاصه ۲ تا ۳ خطی آغاز می‌شود |
| صحت | فقط محتوای کد واقعی؛ در صورت نبود شواهد، عبارت «در کد فعلی مشخص نیست» درج می‌شود |
| عدم تکرار | هر موضوع فقط یک سند دارد؛ بقیه اسناد با لینک نسبی به آن ارجاع می‌دهند |
| قالب | Markdown خالص با جدول برای فهرست‌ها؛ بدون HTML و تصویر |
| اعداد | تعداد فایل، Endpoint و مانند آن از فایل‌سیستم استخراج می‌شود، نه از تخمین |
