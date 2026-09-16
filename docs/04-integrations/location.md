[بازگشت به فهرست یکپارچه‌ها](README.md)

# داده مکان (استان و شهر)

استان و شهر از یک API بیرونی خوانده می‌شوند و هیچ جدول یا Aggregate محلی برای آن‌ها وجود ندارد.
`Application/Location` دو Query و یک قرارداد دارد و `Infrastructure/Location` پیاده‌سازی HTTP آن را
فراهم می‌کند. این داده در آدرس تحویل سفارش استفاده می‌شود
([03-modules/orders.md](../03-modules/orders.md)).

## ساختار

| بخش | محتوا |
|---|---|
| `Application/Location/Contracts/` | `ILocationService` با دو متد `GetProvincesAsync` و `GetCitiesByProvinceAsync` |
| `Application/Location/Features/Queries/GetStates/` | `GetStatesQuery` + `GetStatesHandler` |
| `Application/Location/Features/Queries/GetCities/` | `GetCitiesQuery` + `GetCitiesHandler` |
| `Application/Location/Features/Shared/` | `ProvinceDto(Id, Name, Code)` و `CityDto(Id, Name, Province, StateId)` |
| `Infrastructure/Location/Services/` | `LocationService` |
| `Infrastructure/Location/Models/` | `ExternalProvinceApiDto` و `ExternalCityApiDto` (internal) |
| `Presentation/Location/` | `LocationController` و فایل خالی `LocationRequests.cs` |

## منبع داده و کلاینت HTTP

اتصال در `AddCommunicationServices` (`InfrastructureServiceExtensions.cs`) با Typed HttpClient ثبت می‌شود:

| جنبه | مقدار |
|---|---|
| BaseAddress | `https://iran-locations-api.ir/api/v1/fa/` (هاردکد در کد) |
| Timeout | ۱۵ ثانیه |
| Redirect | `AllowAutoRedirect = true` با حداکثر ۵ بار |
| سیاست تاب‌آوری | **هیچ retry یا Circuit Breaker ثبت نشده** (برخلاف کلاینت‌های کاوه‌نگار و زرین‌پال) |
| Endpointها | `states` و `cities?state_id={provinceId}` |

## جریان درخواست

1. `LocationController` (`api/v{version:apiVersion}/locations`، `[AllowAnonymous]`) یکی از دو اکشن
   `GET states` یا `GET cities?stateId=` را می‌پذیرد.
2. Query مربوطه از `ICacheableQuery` ارث می‌برد و `CachingBehavior` پاسخ را کش می‌کند:

| Query | CacheKey | TTL |
|---|---|---|
| `GetStatesQuery(Page = 1, PageSize = 50)` | `location:states:page={Page}:size={PageSize}` | ۲۴ ساعت |
| `GetCitiesQuery(StateId)` | `location:cities:state={StateId}` | ۲۴ ساعت |

3. Handler به `ILocationService` می‌رسد و `LocationService` با `GetFromJsonAsync` فهرست استان/شهر را
   می‌خواند و DTOهای بیرونی را به `ProvinceDto`/`CityDto` نگاشت می‌کند.
4. در خطا، پیام‌های ثابت `Failed to fetch provinces from the location API.` و
   `Failed to fetch cities for province {StateId} from the location API.` در حسابرسی ثبت و استثنا
   مجدداً پرتاب می‌شود (شامل تبدیل استثنای توأم‌شده با `HttpRequestException`).

نکته‌های پیاده‌سازی:

- `GetStatesHandler` صفحه‌بندی واقعی انجام نمی‌دهد؛ کل فهرست را در یک صفحه برمی‌گرداند
  (`PaginatedResult.Create(list, list.Count, 1, list.Count)`).
- `GetCitiesHandler` مقدار `StateId` عددی را به رشته تبدیل و به‌عنوان `state_id` می‌فرستد.
- `ProvinceDto.Code` در نبود مقدار در پاسخ بیرونی، رشته خالی می‌شود؛ `CityDto.Province` هم همین‌طور.

## کش و وابستگی بیرونی

- چون داده در دیتابیس نیست، **همه درخواست‌ها به در دسترس بودن `iran-locations-api.ir` وابسته‌اند**؛
  تنها سپر، کش ۲۴ ساعته‌ی پاسخ Query است (در حالت `Cache:UseRedis=false` این کش درون‌حافظه‌ای و
  per-instance است — [cache-redis.md](cache-redis.md)).
- خروجی `GET states` ساختار `PaginatedResult<ProvinceDto>` و خروجی `GET cities` ساختار
  `IEnumerable<CityDto>` دارد؛ قراردادهای پاکت پاسخ در
  [05-api/conventions.md](../05-api/conventions.md).

## نکات و محدودیت‌ها

- هیچ Options یا کلید تنظیماتی برای آدرس/مهلت این API وجود ندارد؛ تغییر محیط (مثلاً نسخه انگلیسی API)
  نیازمند ویرایش کد است.
- داده استان/شهر در دیتابیس ذخیره نمی‌شود؛ بنابراین اعتبارسنجی «استان/شهر معتبر» در زمان ثبت آدرس به
  پاسخ همین API وابسته است ([03-modules/identity/users.md](../03-modules/identity/users.md)).
- `LocationRequests.cs` خالی است و کلاس درخواستی برای این کنترلر وجود ندارد.
- در کد فعلی مشخص نیست آیا سرویس بیرونی محدودیت نرخ یا کلید دسترسی دارد؛ تنظیمات مرتبطی در appsettings
  دیده نمی‌شود.

## اسناد مرتبط

- آدرس تحویل و ثبت سفارش: [03-modules/orders.md](../03-modules/orders.md)
- آدرس‌های کاربر: [03-modules/identity/users.md](../03-modules/identity/users.md)
- کش و TTLها: [cache-redis.md](cache-redis.md)
- تنظیمات و کلاینت‌های HTTP بیرونی: [01-getting-started/configuration.md](../01-getting-started/configuration.md)
