[بازگشت به فهرست ماژول‌ها](README.md)

# انبار، موجودی و دفتر انبار (Inventory)

این ماژول مالک موجودی است: `Inventory` به‌ازای هر واریانت، `Warehouse` برای انبارها و
`StockLedgerEntry` به‌عنوان دفتر فقط-افزودنی هر حرکت موجودی. Application آن ۱۴ Command و ۱۳ Query دارد و
۲۴ اکشن در سه کنترلر ارائه می‌کند. رزرو و تأیید موجودی در جریان سفارش از [orders.md](orders.md) و
اتصال واریانت از [variants.md](variants.md) آمده است.

## دامنه (`Domain/Inventory`)

### Aggregate موجودی

`Inventory : AggregateRoot<InventoryId>, ISoftDeletable` با `StockQuantity`, `ReservedQuantity`,
`IsUnlimited`, `LowStockThreshold` (پیش‌فرض ۵), `VariantId` و مجموعه `_ledgerEntries`.

ویژگی‌های محاسبه‌شده: `AvailableQuantity` (در حالت نامحدود `int.MaxValue`)، `IsInStock`, `IsOutOfStock`,
`IsLowStock` (فقط برای موجودی محدود و `0 < Available <= threshold`) و `CanFulfill(int)`.

| متد | قاعده/اثر |
|---|---|
| `Create` | موجودی اولیه و آستانه منفی ممنوع؛ در صورت موجودی اولیه مثبت، ردیف دفتر `StockIn` می‌نویسد؛ صدور `InventoryCreatedEvent` |
| `IncreaseStock` | مقدار باید مثبت باشد؛ `IsUnlimited` را خاموش می‌کند؛ دفتر `StockIn`؛ صدور `StockIncreasedEvent` |
| `DecreaseStock` | در حالت نامحدود رد می‌شود؛ بدون منفی‌شدن (`TrySubtract`)؛ دفتر `Adjustment` با مقدار منفی؛ صدور `StockDecreasedEvent` |
| `ReserveStock` | جلوگیری از رزرو بیش از موجودی آزاد؛ افزایش `ReservedQuantity`؛ دفتر `Reserve`؛ صدور `StockReservedEvent` |
| `ReleaseReservation` | آزادسازی `min(quantity, ReservedQuantity)`؛ دفتر `ReleaseReservation`؛ صدور `StockReservationReleasedEvent` |
| `ConfirmReservation` | الزام `ReservedQuantity >= quantity` (وگرنه `Inventory.InsufficientReservation`)؛ کاهش هم‌زمان موجودی و رزرو؛ دفتر `CommitReservation`؛ صدور `StockCommittedEvent` |
| `AdjustStock` / `AdjustStockTo` | تنظیم دستی؛ `AdjustStockTo` اختلاف را به `IncreaseStock`/`DecreaseStock` می‌سپارد؛ دفتر `Adjustment`؛ صدور `StockAdjustedEvent` |
| `ReturnStock` | برگرداندن مرجوعی به موجودی؛ دفتر `StockIn`؛ صدور `StockRestoredEvent` |
| `RecordDamage` | کسر ضایعات با یادداشت «ضایعات: {reason}»؛ **رویدادی صادر نمی‌کند** |
| `Reconcile` | هم‌ترازی موجودی با مقدار محاسبه‌شده از دفتر؛ **رویدادی صادر نمی‌کند** |
| `ReverseStockChange` | برگشت یک تراکنش بر اساس `IdempotencyKey` با اعمال معکوس مقدار |
| `SetUnlimited` / `SetLowStockThreshold` | نامحدودسازی (با رویداد) و تغییر آستانه (آستانه منفی ممنوع) |

### Aggregate انبار و Entity دفتر

`Warehouse : AggregateRoot<WarehouseId>, IActivatable, IAuditable` با `Code` (۲ تا ۲۰ کاراکتر، الگوی
`^[A-Z0-9\-_]+$` و یکتا)، `Name`, `City`, `Address?`, `Phone?`, `IsActive`, `IsDefault`, `Priority`
و متدهای `Create`, `Update`, `SetAsDefault`, `ClearDefault`, `Activate`, `Deactivate`.
**این Aggregate هیچ Domain Event صادر نمی‌کند.**

`StockLedgerEntry : Entity<StockLedgerEntryId>, IAuditable` با `VariantId`, `WarehouseId?`, `OrderItemId?`,
`UserId?`, `EventType`, `QuantityDelta`, `BalanceAfter`, `UnitCost`, `ReferenceNumber`, `CorrelationId`,
`Note`, `IdempotencyKey`. کارخانه‌ها: `StockIn`, `Reserve`, `ReleaseReservation`, `CommitReservation`,
`Adjustment`. قاعده سخت: `BalanceAfter` منفی ممنوع است. کلید Idempotency با الگوی
`{variantId}:{eventType}:{referenceNumber ?? Guid}` ساخته می‌شود و در دیتابیس ایندکس یگانه دارد.
`StockEventType`: `StockIn=1, Sale=2, Reservation=3, ReservationRelease=4, ReservationCommit=5,
Adjustment=6, Return=7, Damage=8, Transfer=9, InitialStock=10` — از این میان فقط
`StockIn`, `Reservation`, `ReservationRelease`, `ReservationCommit` و `Adjustment` در کد نوشته می‌شوند.

`StockQuantity`: بازه ۰ تا ۱٬۰۰۰٬۰۰۰ (`MaxStockValue`)؛ `Subtract` در کمبود خطای «موجودی کافی نیست...»
می‌دهد و `TrySubtract` نتیجه `ServiceResult` برمی‌گرداند.

### Domain Eventها (۱۱)

`InventoryCreatedEvent`, `StockIncreasedEvent`, `StockDecreasedEvent`, `StockReservedEvent`,
`StockReservationReleasedEvent`, `StockCommittedEvent`, `StockAdjustedEvent`, `StockSetUnlimitedEvent`,
`StockRestoredEvent`, `StockReleasedEvent` و `StockReturnedEvent` — **دو رویداد آخر هیچ‌جا صادر نمی‌شوند**.
رویداد «کمبود موجودی» وجود ندارد و کمبود فقط از طریق Query قابل مشاهده است.
پوشه `Exceptions/` خالی است (خطاها با `DomainException` عمومی یا `ServiceResult` برگردانده می‌شوند) و
پوشه `Rules/` وجود ندارد.

### سرویس‌های دامنه

- `InventoryDomainService` — نمای ایستا برای عملیات موجودی (Reserve/Confirm/Rollback/Return/Adjust/Damage/Reconcile/Increase/Decrease).
- `InventoryReservationService : IInventoryReservationService` — عملیات گروهی Validate/Reserve/Release؛
  اعتبارسنجی آن فقط «مقدار مثبت» هر آیتم را می‌سنجد و در Domain به Repository دسترسی ندارد.

## Application (`Application/Inventory`)

**۱۴ Command:** `AddStock`, `RemoveStock`, `AdjustStock`, `BulkAdjustStock`, `BulkStockIn`,
`CommitStockForOrder`, `ReconcileStock`, `RecordDamage`, `ReverseInventoryTransaction`,
`CreateWarehouse`, `UpdateWarehouse`, `DeleteWarehouse`, `SetDefaultWarehouse`, `ToggleWarehouseActive`.
Validatorها برای `AdjustStock`, `BulkAdjustStock`, `ReconcileStock` و `RecordDamage` وجود دارند.

**۱۳ Query:** `GetInventory`, `GetInventoryStatus`, `GetVariantAvailability` (کش دستی
`inventory:availability:{variantId}` با انقضای ۲ دقیقه), `GetBatchVariantAvailability`,
`GetProductInventoryStatuses`, `GetStockLedgerByVariant`, `GetInventoryTransactions`,
`GetLowStockProducts` (آستانه پیش‌فرض ۵), `GetOutOfStockProducts`, `GetInventoryStatistics`,
`GetWarehouseById`, `GetWarehouseStock`, `GetAllWarehouses` (`ICacheableQuery` با کلید `warehouses:all`
و انقضای یک ساعت).

این ماژول در Application Event Handler ندارد؛ دو Handler در لایه زیرساخت هستند:
`InventoryStockChangedCacheHandler` (ابطال کش موجودی برای رویدادهای رزرو/آزادسازی/تأیید/تنظیم/برگشت) و
`InventoryStockSearchSyncHandler` (ثبت Outbox جست‌وجو با نوع تغییر `StockChanged`).

نکات Handlerها: `RemoveStockHandler` کش `product:{productId}` و `variant:{variantId}` را باطل می‌کند و
`BulkStockInHandler` در نبود کاربر جاری همه آیتم‌ها را بی‌صدا رد می‌کند (`return 0`).

## API

جمع: **۳ کنترلر و ۲۴ اکشن**. قراردادهای عمومی در [05-api/conventions.md](../05-api/conventions.md).

`AdminInventoryController` — `[Route("api/v{version:apiVersion}/admin/inventory")]` — `[Authorize(Roles = "Admin")]`؛ ۱۵ اکشن:

| متد | مسیر | اکشن |
|---|---|---|
| GET | `transactions` | `GetInventoryTransactions` |
| GET | `ledger` | `GetStockLedger` |
| GET | `variants/{variantId:guid}/warehouse-stock` | `GetWarehouseStock` |
| GET | `low-stock` | `GetLowStockItems` |
| GET | `out-of-stock` | `GetOutOfStockItems` |
| GET | `statistics` | `GetStatistics` |
| GET | `variants/{variantId:guid}/status` | `GetInventoryStatus` |
| GET | `products/{productId:guid}/statuses` | `GetProductInventoryStatuses` |
| POST | `transactions/reversal` | `ReverseTransaction` |
| POST | `adjustments` | `AdjustStock` |
| POST | `adjustments/bulk` | `BulkAdjustStock` |
| POST | `variants/{variantId:guid}/reconciliation` | `ReconcileStock` |
| POST | `damage-records` | `RecordDamage` |
| POST | `imports` | `BulkStockIn` |
| PATCH | `orders/{orderId:guid}/return` | `ApproveReturn` (Command ماژول Order را می‌فرستد) |

`InventoryController` — `[Route("api/v{version:apiVersion}/inventory")]` — `[AllowAnonymous]`؛ ۲ اکشن:
`GET availability/{variantId:guid}` → `GetVariantAvailability`، `POST availability/batch` → `GetBatchAvailability`.

`AdminWarehouseController` — `[Route("api/v{version:apiVersion}/admin/warehouses")]` — `[Authorize(Roles = "Admin")]`؛ ۷ اکشن:
`GET` → `GetAll`، `GET {id:guid}` → `GetById`، `POST` → `Create`، `PUT {id:guid}` → `Update`،
`DELETE {id:guid}` → `Delete`، `PATCH {id:guid}/set-default-warehouse` → `SetDefaultWarehouse`،
`PATCH {id:guid}/status` → `ToggleStatus`.

## زیرساخت

- `InventoryConfiguration` ایندکس یگانه روی `VariantId`، `WarehouseConfiguration` ایندکس یگانه روی `Code`
  و `StockLedgerEntryConfiguration` ایندکس یگانه روی `IdempotencyKey` دارد؛ ایندکس‌های `VariantId` و
  `(VariantId, CreatedAt desc)` برای پرس‌وجوهای دفتر ساخته شده‌اند.
- `InventoryReservationExpiryJob` (`Infrastructure/BackgroundJobs/`) هر ۵ دقیقه با قفل توزیع‌شده
  `jobs:inventory-reservation-expiry` ردیف‌های `Reservation` قدیمی‌تر از `ReservationExpiryOptions.ExpiryMinutes`
  (پیش‌فرض ۳۰ دقیقه) را که Release/Commit نشده‌اند آزاد می‌کند («آزادسازی خودکار رزرو منقضی‌شده»).
- همگام‌سازی جست‌وجو از مسیر Outbox اختصاصی Elasticsearch انجام می‌شود (به [search.md](search.md)).
- مصرف‌کنندگان این ماژول در سایر ماژول‌ها: Saga سفارش (`ReserveStockAsync`، `CommitStockForOrderCommand`،
  آزادسازی در لغو/انقضا)، `ApproveReturn` در Order و Endpointهای `stock` در کنترلر واریانت.

## نکات و محدودیت‌ها

- `RecordDamage` و `Reconcile` موجودی را تغییر می‌دهند اما **رویدادی صادر نمی‌کنند**؛ بنابراین نه کش
  invalidate می‌شود و نه همگام‌سازی جست‌وجو برای این دو عملیات رخ می‌دهد.
- `StockReleasedEvent` و `StockReturnedEvent` هرگز صادر نمی‌شوند، در حالی که Handler کش و جست‌وجو به
  آن‌ها گوش می‌دهند (`InventoryStockChangedCacheHandler` و `InventoryStockSearchSyncHandler`)؛
  این بخش‌ها عملاً بی‌اثرند.
- Endpoint `POST admin/inventory/variants/{variantId}/reconciliation` مقدار موجودی محاسبه‌شده را
  **همیشه صفر** می‌فرستد (`ReconcileStockCommand(variantId, 0)`)، بنابراین انبارگردانی از این مسیر
  موجودی را صفر می‌کند.
- انبار (`Warehouse`) در عملیات رزرو/تأیید استفاده نمی‌شود؛ `WarehouseId` در دفتر اختیاری است و
  موجودی در سطح واریانت نگهداری می‌شود، نه به‌ازای هر انبار.
- سقف موجودی ۱٬۰۰۰٬۰۰۰ واحد در `StockQuantity` است و عبور از آن خطا می‌دهد.
- در working tree فعلی متدها و `InventoryDomainService` پارامتر `DateTime now` گرفته‌اند ولی بخشی از
  فراخوانی‌ها (از جمله `AdjustStockTo`) هنوز امضای قبلی را صدا می‌زنند؛ ناسازگاری موقتی دو لایه است.

## اسناد مرتبط

- رزرو/تأیید در Saga سفارش: [orders.md](orders.md)
- واریانت و SKU: [variants.md](variants.md) — محصول: [products.md](products.md)
- Jobها، Outbox و کش: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
