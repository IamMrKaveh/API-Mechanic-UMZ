[بازگشت به فهرست ماژول‌ها](README.md)

# آمار و داشبورد (Analytics)

ماژول Analytics یک لایه گزارش‌گیری فقط-خواندنی روی جداول موجود است و هیچ داده‌ای تولید یا رهگیری
نمی‌کند: آمار داشبورد، نمودار فروش، محصولات پرفروش، عملکرد دسته‌ها، گزارش درآمد و گزارش انبار.
این ماژول Domain و Command ندارد؛ Application آن ۶ Query و API آن ۶ اکشن مدیریتی است.
آمار داشبورد کاربر در [identity/users.md](identity/users.md) و موجودی در [inventory.md](inventory.md) آمده است.

## ساختار

`Domain/Analytics` وجود ندارد و ماژول هیچ موجودیت، رویداد یا جدول اختصاصی ندارد. پوشه ارائه آن در کد
**`Presentation/Analytic`** (نام‌فضای `Presentation.Analytic`) است، نه `Presentation/Analytics`.
کلاس‌های درخواست در `Presentation/Analytic/Requests/AnalyticRequests.cs` و نگاشت در
`Mapping/AnalyticMappingConfig.cs` قرار دارند.

## Application (`Application/Analytics`)

**۰ Command** (پوشه Commands وجود ندارد) و **۶ Query** در `Features/Queries/`:

| Query | ورودی‌ها |
|---|---|
| `GetDashboardStatistics` | `FromDate?`, `ToDate?` |
| `GetSalesChartData` | `FromDate`, `ToDate`, `GroupBy` (پیش‌فرض `"day"`), صفحه‌بندی |
| `GetTopSellingProducts` | `Count` (پیش‌فرض ۱۰)، بازه زمانی، صفحه‌بندی |
| `GetCategoryPerformance` | بازه زمانی، صفحه‌بندی |
| `GetRevenueReport` | `FromDate`, `ToDate` |
| `GetInventoryReport` | بدون ورودی |

پنج Validator وجود دارد (همه به‌جز `GetInventoryReport`). `GetDashboardStatisticsHandler` خروجی را
با `ICacheService` و کلید `analytics:dashboard:{from}:{to}` به مدت ۱۰ دقیقه کش می‌کند.
قرارداد `IAnalyticsQueryService` شش متد متناظر دارد.

DTOهای خروجی (`Features/Shared/AnalyticsDtos.cs`): `DashboardStatisticsDto` (۱۷ فیلد شامل شمارش سفارش
به‌تفکیک وضعیت، درآمد کل و نرخ لغو), `SalesChartDataPointDto`, `TopSellingProductDto`,
`CategoryPerformanceDto`, `RevenueReportDto`, `RevenueByStatusDto`, `InventoryReportDto`.
**این ماژول Event Handler ندارد.**

## API

جمع: **۱ کنترلر و ۶ اکشن**، همه `GET` و مدیریتی. قراردادهای عمومی در
[05-api/conventions.md](../05-api/conventions.md).

`AdminAnalyticsController` — `[Route("api/v{version:apiVersion}/admin/analytics")]` — `[Authorize(Roles = "Admin")]`:

| متد | مسیر | اکشن |
|---|---|---|
| GET | `dashboard` | `GetDashboardStatistics` |
| GET | `sales-chart` | `GetSalesChartData` |
| GET | `top-products` | `GetTopSellingProducts` |
| GET | `category-performance` | `GetCategoryPerformance` |
| GET | `revenue` | `GetRevenueReport` |
| GET | `inventory` | `GetInventoryReport` |

کنترلر عمومی برای گزارش‌ها وجود ندارد؛ همه گزارش‌ها فقط برای نقش `Admin` در دسترس‌اند.

## زیرساخت

- تنها فایل زیرساخت `Infrastructure/Analytics/QueryServices/AnalyticsQueryService.cs` است
  (`sealed class AnalyticsQueryService(DBContext) : IAnalyticsQueryService`) که همه گزارش‌ها را با
  LINQ و EF Core می‌سازد؛ **SQL خام در این ماژول وجود ندارد**.
- منابع داده: `Orders`, `OrderItems`, `Users`, `Products`, `Inventories`.
  داشبورد: تعداد سفارش و درآمد در بازه، کاربران جدید و کل کاربران/محصولات.
  درآمد: مبلغ ناخالص، تخفیف، ارسال، خالص، میانگین و تفکیک بر اساس وضعیت.
  انبار: تعداد کل/موجود/ناموجود/کمبود واریانت‌ها با آستانه کمبود پیش‌فرض ۵.
  محصولات پرفروش: گروه‌بندی `OrderItems` بر اساس محصول/نام/SKU. نمودار فروش: گروه‌بندی روز/هفته/ماه.
- ثبت سرویس با جاروب اسمبلی انجام می‌شود: کلاس‌های با پسوند `QueryService` به Interface متناظر
  متصل می‌شوند (`InfrastructureServiceExtensions.AddQueryServices`).
- Job پس‌زمینه یا جدول تجمیعی (materialized) برای آمار وجود ندارد؛ هر درخواست روی داده زنده محاسبه می‌شود.

## نکات و محدودیت‌ها

- **رهگیری رفتار کاربر وجود ندارد**: بازدید صفحه، بازدید محصول یا قیف تبدیل ثبت نمی‌شود؛ همه گزارش‌ها
  از داده تراکنشی موجود ساخته می‌شوند.
- فقط داشبورد کش می‌شود (۱۰ دقیقه)؛ سایر گزارش‌ها هر بار کامل محاسبه می‌شوند و برای بازه‌های بزرگ
  هزینه‌بر خواهند بود.
- `GetSalesChartData` مقادیر `GroupBy` را می‌پذیرد و گروه‌بندی روز/هفته/ماه را پشتیبانی می‌کند؛ سقف
  تعداد نقاط نمودار یا sampling در کد اعمال نمی‌شود.
- DTO داشبورد فیلد «کاربران جدید» را در بازه محاسبه می‌کند؛ مبنای آن تاریخ ساخت کاربر است، نه ورود او.
- مرز ماژول: آمار داشبورد **کاربر** (`GET api/v1/dashboard`) در ماژول User است
  ([identity/users.md](identity/users.md)) و این ماژول فقط گزارش‌های مدیریتی را پوشش می‌دهد.

## اسناد مرتبط

- داشبورد کاربر: [identity/users.md](identity/users.md)
- سفارش‌ها و وضعیت‌ها: [orders.md](orders.md) — درآمد و پرداخت: [payments.md](payments.md)
- گزارش انبار و آستانه کمبود: [inventory.md](inventory.md)
- کش و آمار عملکرد: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
