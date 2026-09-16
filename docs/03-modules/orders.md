[بازگشت به فهرست ماژول‌ها](README.md)

# سفارش‌ها و چرخه وضعیت (Orders)

ماژول `Order` قلب فروشگاه است: ثبت سفارش از سبد خرید، ماشین وضعیت دوازده‌حالته، Saga هماهنگی
رزرو/تأیید موجودی و پرداخت، انقضای سفارش و جریان مرجوعی. Application آن هجده Command و هفت Query دارد و
۲۴ اکشن در پنج کنترلر ارائه می‌کند. پرداخت در [payments.md](payments.md)، موجودی در
[inventory.md](inventory.md) و سبد در [cart.md](cart.md) مستند شده‌اند.

## دامنه (`Domain/Order`)

### Aggregate و Entityها

- `Order : AggregateRoot<OrderId>` با `OrderNumber`, `Status`, `ReceiverInfo`, `DeliveryAddress`,
  `SubTotal`/`ShippingCost`/`DiscountAmount`/`FinalAmount` (از نوع `Money`)، `IdempotencyKey`,
  `CancellationReason`, `UserId`, `AppliedDiscountCodeId`, `PaymentTransactionId`, `PaymentMethodId`،
  `DeliveredAt` و مجموعه `OrderItems`.
- `OrderItem : Entity<OrderItemId>` — تصویر لحظه‌ای نام محصول، SKU و قیمت واحد (غیرقابل تغییر بعد از ثبت)
  همراه با `Quantity`.
- `OrderStatus : AggregateRoot<OrderStatusId>` — **تعریف** وضعیت‌ها (نه وضعیت یک سفارش): `Name`,
  `DisplayName`, `Icon`, `Color`, `SortOrder`, `IsActive`, `IsDefault`, `AllowCancel`, `AllowEdit`.
  متدها: `Create`، `Update`، `Activate`/`Deactivate` (وضعیت پیش‌فرض قابل غیرفعال‌سازی نیست)،
  `SetAsDefault` (وضعیت غیرفعال نمی‌تواند پیش‌فرض شود)، `UnsetAsDefault`، `MarkAsDeleted`.

### ValueObjectها

| ValueObject | قاعده |
|---|---|
| `OrderNumber` | قالب `ORD-{yyyyMMdd}-{۸ کاراکتر GUID}` و یکتا |
| `OrderStatusValue` | Smart-Enum با ۱۲ نمونه؛ نگهدارنده ماشین وضعیت |
| `ReceiverInfo` | نام + شماره تماس نرمال‌شده به ۱۰ تا ۱۵ رقم |
| `DeliveryAddress` | استان، شهر، خیابان و کد پستی، همه الزامی |
| `OrderItemSnapshot` | ورودی ساخت آیتم‌ها؛ تعداد باید بزرگ‌تر از صفر و همه فیلدها الزامی |

### ماشین وضعیت (`OrderStatusValue`)

وضعیت‌ها با ترتیب عددی: `Created(0)`, `Reserved(1)`, `Pending(2)`, `Failed(3)`, `Paid(4)`,
`Processing(5)`, `Shipped(6)`, `Delivered(7)`, `Cancelled(8)`, `Returned(9)`, `Refunded(10)`, `Expired(11)`.
`Delivered`, `Cancelled`, `Returned`, `Refunded` و `Expired` پایانی (`IsFinal`) هستند.

انتقال‌های مجاز:

| از | به |
|---|---|
| `Created` | `Reserved`, `Pending`, `Paid`, `Cancelled`, `Expired` |
| `Reserved` | `Pending`, `Cancelled`, `Expired` |
| `Pending` | `Paid`, `Failed`, `Cancelled`, `Expired` |
| `Failed` | `Pending`, `Cancelled`, `Expired` |
| `Paid` | `Processing`, `Cancelled`, `Refunded` |
| `Processing` | `Shipped`, `Cancelled` |
| `Shipped` | `Delivered`, `Returned` |
| `Delivered` | `Returned`, `Refunded` |
| `Returned` | `Refunded` |
| `Cancelled` / `Refunded` / `Expired` | هیچ‌کدام (پایانی) |

پرچم‌های مشتق: `IsPaid` برای `Paid|Processing|Shipped|Delivered`؛ `CanBeCancelled()` برابر
«پایانی نبودن و `Shipped` نبودن»؛ `CanBeEdited()` فقط `Created|Reserved|Pending`.

متدهای انتقال روی Aggregate: `MoveToPending`, `MarkAsPaid`, `StartProcessing`, `MarkAsShipped`,
`MarkAsDelivered`, `Cancel`, `Expire`, `Refund`, `MarkAsReturned` — همه از مسیر خصوصی `TransitionTo`
عبور می‌کنند که `CanTransitionTo` را می‌سنجد و `OrderStatusChangedEvent` صادر می‌کند.

### Exceptionها

| Exception | `ErrorCode` | قاعده |
|---|---|---|
| `EmptyOrderException` | `EMPTY_ORDER` | سفارش باید حداقل یک آیتم داشته باشد |
| `InvalidOrderTransitionException` | `INVALID_ORDER_TRANSITION` | انتقال نامجاز بین وضعیت‌ها |
| `OrderCancellationNotAllowedException` | `ORDER_CANCELLATION_NOT_ALLOWED` | لغو در وضعیت فعلی مجاز نیست |
| `OrderAlreadyPaidException` | `ORDER_ALREADY_PAID` | تعریف شده اما در متدهای Aggregate پرتاب نمی‌شود (مصرف در پرداخت) |
| `OrderNotFoundException` | `ORDER_NOT_FOUND` | سفارش یافت نشد |

### Domain Eventها (۱۰)

`OrderCreatedEvent`, `OrderStatusChangedEvent`, `OrderPaidEvent`, `OrderCancelledEvent`,
`OrderExpiredEvent` (در کد اجرایی صادر نمی‌شود؛ فقط Saga به آن گوش می‌دهد), `OrderStatusCreatedDomainEvent`,
`OrderStatusUpdatedDomainEvent`, `OrderStatusActivationChangedDomainEvent`,
`OrderStatusDefaultChangedDomainEvent`, `OrderStatusDeletedDomainEvent`.

پوشه‌های `Rules/`، `Services/` و `Results/` این ماژول خالی‌اند.

## Application (`Application/Order`)

**۱۸ Command:** `CheckoutFromCart`, `CreateOrder`, `UpdateOrder`, `CancelOrder`, `ConfirmDelivery`,
`RequestReturn`, `ApproveReturn`, `MarkOrderAsShipped`, `DeleteOrder`, `DeleteOrderItem`, `ExpireOrders`,
`CreateOrderStatus`, `UpdateOrderStatus`, `UpdateOrderStatusDefinition`, `DeleteOrderStatus`,
`ActivateOrderStatus`, `DeactivateOrderStatus`, `SetDefaultOrderStatus`.

**۷ Query:** `GetUserOrders`, `GetOrderDetails`, `GetAdminOrders`, `GetAdminOrderById`,
`GetOrderStatistics`, `GetOrderStatuses` (`ICacheableQuery` با کلید `order-status:list:onlyActive=...`
و انقضای ۱۰ دقیقه), `GetOrderStatus`.

**Event Handler (۱ کلاس):** `OrderPlacedNotificationEventHandler` روی `OrderCreatedEvent` اعلان
«سفارش ثبت شد» با لینک `/dashboard/orders/{id}` می‌سازد و خطاها را می‌بلعد.

### زنجیره ثبت سفارش (`CheckoutFromCart`)

مدارک واقعی این جریان در `Infrastructure/Order/Services/CheckoutOrchestrationService.cs` است و
Command آن از `POST api/v{version:apiVersion}/orders` آغاز می‌شود. مراحل به‌ترتیب:

1. انتخاب استراتژی پرداخت با `CheckoutPaymentStrategyResolver`: ابتدا بر اساس `PaymentMethodId` و
   کد روش، سپس بر اساس `PaymentGateway`؛ کدهای شناخته‌شده `zarinpal-sandbox`, `zarinpal`,
   `cash-on-delivery`, `wallet` هستند.
2. تعیین گیرنده و آدرس (`CheckoutAddressResolverService`).
3. ساخت آیتم‌ها از سبد (`CheckoutCartItemBuilderService`) — الزام وجود سبد، تعلق آن به کاربر جاری
   (وگرنه Forbidden) و خالی‌نبودن.
4. اعتبارسنجی موجودی (`CheckoutStockValidatorService`).
5. اعتبارسنجی قیمت (`CheckoutPriceValidatorService`).
6. اعتبارسنجی روش ارسال و محاسبه هزینه (`CheckoutShippingValidatorService`).
7. اعمال تخفیف (`CheckoutDiscountApplicatorService`).
8. ساخت سفارش (`CheckoutOrderCreationService`): بررسی Idempotency، `Order.Place(...)` و ذخیره؛
   **`OrderCreatedEvent` همین‌جا صادر می‌شود و Saga رزرو موجودی را آغاز می‌کند.**
9. اجرای استراتژی پرداخت:
   - `cash-on-delivery`: تنها `order.MoveToPending()`؛ پرداختی انجام نمی‌شود.
   - `wallet`: بررسی Idempotency، `wallet.Debit(...)` و سپس تسویه سفارش به‌عنوان پرداخت‌شده
     (خطاهای موجودی ناکافی/کیف غیرفعال/همزمانی گرفته می‌شوند)؛ مبلغ صفر یا کمتر مسیر «سفارش رایگان» را می‌رود.
   - `zarinpal`: `paymentService.InitiatePaymentAsync(...)` و بازگشت `PaymentUrl`/`Authority`؛ سفارش تا
     بازگشت از درگاه در وضعیت `Pending` می‌ماند (به [payments.md](payments.md)).
10. `cart.Checkout()` و ذخیره؛ صدور `CartCheckedOutEvent`.

شکست در هر یک از مراحل ۱ تا ۸ به `ServiceResult.Failure` می‌رسد و چیزی ذخیره نمی‌شود.

### Saga فرآیند سفارش

`OrderProcessManagerSaga` (`Application/Order/Sagas/OrderProcessManagerSaga.cs`) یک Process Manager مبتنی بر
MediatR است که به پنج رویداد گوش می‌دهد: `OrderCreatedEvent`, `PaymentSucceededEvent`,
`PaymentFailedEvent`, `OrderCancelledEvent`, `OrderExpiredEvent`. وضعیت آن در
`OrderProcessState` (`Application/Order/Sagas/State/`) با فیلدهای `OrderId`, `CurrentStep`, `Status`,
`FailureReason`, `RetryCount`, `CorrelationId` نگهداری و در جدول `OrderProcessStates` با **ایندکس یگانه
روی `OrderId`** ذخیره می‌شود (`Infrastructure/Order/Repositories/OrderProcessStateRepository.cs`).

مراحل (`ProcessStepEnum`): `Created, InventoryReserving, InventoryReserved, PaymentPending,
PaymentSucceeded, InventoryCommitting, InventoryCommitFailed, Refunded, RequiresManualReconciliation,
Completed, Compensating, Compensated, Failed`.
وضعیت‌ها (`ProcessStatusEnum`): `InProgress, Completed, Failed, Compensating, Compensated`.

| رویداد محرک | گام‌ها |
|---|---|
| `OrderCreatedEvent` | ساخت وضعیت → `InventoryReserving` → `ReserveStockAsync` برای هر آیتم با مرجع `ORDER-{orderId}`؛ موفقیت → `InventoryReserved`، خطا → `Compensating`/`Compensated` و سپس `Failed` |
| `PaymentSucceededEvent` | `PaymentSettlementService.ProcessPaymentSuccess` → `PaymentSucceeded` → `InventoryCommitting` → `CommitStockForOrderCommand`؛ موفقیت → `Completed`؛ خطا و فعال‌بودن فلگ `SagaAutoRefundOnCommitFailure` → `InitiateRefundAsync` (موفق: `Refunded`) وگرنه `RequiresManualReconciliation` |
| `PaymentFailedEvent` | اگر وضعیت موجود باشد → `PaymentPending` + افزایش `RetryCount` |
| `OrderCancelledEvent` | `Compensating` → آزادسازی رزروها با دلیل «لغو سفارش» → `Compensated` |
| `OrderExpiredEvent` | همان مسیر با دلیل «انقضای سفارش» (این رویداد در کد اجرایی صادر نمی‌شود) |

**زمان‌سنج/Timeout در Saga وجود ندارد**؛ انقضای رزروها جداگانه با
`InventoryReservationExpiryJob` انجام می‌شود (به [inventory.md](inventory.md)).

### انقضای سفارش

دو مسیر مستقل با منطق دامنه یکسان (`Order.Expire`):

- `ExpireOrdersCommand` از Endpoint مدیریتی `POST admin/orders/expiration`؛
- `ExpiredOrderCleanupJob` (`Infrastructure/BackgroundJobs/`) هر ۳۰ دقیقه با قفل توزیع‌شده
  `jobs:expired-order-cleanup`.

معیار انقضا در `OrderRepository.FindPendingExpiredAsync`: وضعیت در `{Created, Reserved, Pending}` و
`CreatedAt < UtcNow - 30 minutes`.

### مرجوعی

موجودیت RMA جداگانه‌ای وجود ندارد؛ مرجوعی یک انتقال وضعیت به `Returned` است:

- `RequestReturn` (مشتری یا ادمین با بررسی مالکیت) با الزام دلیل حداکثر ۱۰۰۰ کاراکتر و
  `RowVersion` از هدر `If-Match`؛ **موجودی را برنمی‌گرداند.**
- `ApproveReturn` (ادمین، با `IBypassTransactionBehavior`): `MarkAsReturned()`
  و سپس `inventoryService.ReturnStockForOrderAsync(orderId, userId, reason)`.

## API

جمع: **۵ کنترلر و ۲۴ اکشن**. قواعد عمومی مسیر و پاسخ در
[05-api/conventions.md](../05-api/conventions.md)؛ نکته اختصاصی این ماژول استفاده از هدر `If-Match`
برای همزمانی در اکشن‌های تغییردهنده است.

`OrdersController` — `[Route("api/v{version:apiVersion}/orders")]` — `[Authorize]`؛ ۶ اکشن:
`GET` → `GetOrders`، `GET {id:guid}` → `GetOrderById`، `POST` → `CheckoutFromCart`،
`PATCH {id:guid}/cancellation` → `CancelOrder`، `PATCH {id:guid}/delivery-confirmation` → `ConfirmDelivery`،
`PATCH {id:guid}/return-request` → `RequestReturn`.

`AdminOrdersController` — `[Route("api/v{version:apiVersion}/admin/orders")]` — `[Authorize(Roles = "Admin")]`؛ ۷ اکشن:
`GET` → `GetOrders`، `GET {id:guid}` → `GetOrderById`، `GET statistics` → `GetStatistics`،
`POST expiration` → `ExpireOrders`، `PATCH {id:guid}/status` → `UpdateOrderStatus`،
`DELETE {id:guid}` → `DeleteOrder`، `PATCH {id:guid}/ship` → `MarkAsShipped`.

`AdminOrderStatusController` — `[Route("api/v{version:apiVersion}/admin/order-statuses")]` — `[Authorize(Roles = "Admin")]`؛ ۸ اکشن:
`GET` → `GetOrderStatuses`، `GET {id:guid}` → `GetOrderStatus`، `POST` → `CreateOrderStatus`،
`PUT {id:guid}` → `UpdateOrderStatus`، `DELETE {id:guid}` → `DeleteOrderStatus`،
`PATCH {id:guid}/activate` → `ActivateOrderStatus`، `PATCH {id:guid}/deactivate` → `DeactivateOrderStatus`،
`PUT {id:guid}/set-default` → `SetDefaultOrderStatus`.

`OrderStatusController` — `[Route("api/v{version:apiVersion}/order-statuses")]` — `[AllowAnonymous]`؛ ۲ اکشن:
`GET` → `GetOrderStatuses`، `GET {id:guid}` → `GetOrderStatusById`.

`OrderItemsController` — `[Route("api/v{version:apiVersion}/order-items")]` — `[Authorize]`؛ ۱ اکشن:
`DELETE {id:guid}` → `DeleteOrderItem`.

## زیرساخت

- `OrderConfiguration` ستون `xmin` را به‌عنوان توکن همزمانی، ایندکس یگانه روی `IdempotencyKey` و
  `OrderNumber`، ایندکس‌های `UserId`، `(Status, CreatedAt)` و `(UserId, Status)` و فیلتر سراسری
  `!IsDeleted` دارد.
- `OrderStatusSeeder` دوازده تعریف وضعیت را هنگام استارت می‌کارد؛ فقط `Created` هم `AllowCancel` و هم
  `AllowEdit` دارد و `Reserved`/`Pending` اجازه لغو دارند.
- `OrderStatusTransitionResolver` (`Application/Order/Mapping/`) نگاشت محدودتری برای رابط مدیریتی
  ارائه می‌دهد: `Paid→[Processing]`, `Processing→[Shipped]`, `Shipped→[Delivered]` و بقیه خالی.
- `OrderCacheInvalidationHandler` در `Infrastructure/Cache/EventHandlers` به رویدادهای ایجاد/پرداخت/لغو/
  تغییر وضعیت واکنش می‌دهد و کش `order:{id}` و `orders:user:{userId}` را باطل می‌کند.

## نکات و محدودیت‌ها

- `OrderExpiredEvent` در کد اجرایی صادر نمی‌شود؛ Saga شاخه `OrderExpiredEvent` را دارد اما انقضای واقعی
  از مسیر `ExpireOrders`/`ExpiredOrderCleanupJob` و مستقیماً روی Aggregate انجام می‌شود.
- مقدار `InventoryCommitFailed` در `ProcessStepEnum` تعریف شده اما هیچ‌جا تنظیم نمی‌شود.
- `CreateOrderCommand` (سفارش دستی ادمین) Handler و Validator دارد اما به هیچ کنترلری متصل نیست.
- بازپرداخت خودکار در شکست تأیید موجودی وابسته به فلگ `SagaAutoRefundOnCommitFailure` است؛ در حالت
  خاموش سفارش به `RequiresManualReconciliation` می‌رسد.
- `OrderAlreadyPaidException` در متدهای Aggregate پرتاب نمی‌شود (فقط تعریف شده است) و بررسی
  «قبلاً پرداخت‌شده» در `PaymentSettlementService` انجام می‌شود.
- ایجاد سفارش با شناسه پرداخت/کیف پول و موجودی گره خورده است؛ برای درک کامل زنجیره، خواندن
  [payments.md](payments.md) و [wallet.md](wallet.md) لازم است.

## اسناد مرتبط

- پرداخت، درگاه و بازپرداخت: [payments.md](payments.md) — کیف پول: [wallet.md](wallet.md)
- رزرو/تأیید موجودی و دفتر انبار: [inventory.md](inventory.md)
- سبد خرید و ثبت سفارش از سبد: [cart.md](cart.md)
- تخفیف و ارسال: [discounts.md](discounts.md)، [shipping.md](shipping.md)
- Outbox، Jobها و همزمانی: [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
