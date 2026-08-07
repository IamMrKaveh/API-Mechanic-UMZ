using Domain.Discount.ValueObjects;
using Domain.Order.Aggregates;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class OrderBuilder
{
    private OrderId _orderId = OrderId.NewId();
    private UserId _userId = UserId.NewId();
    private ReceiverInfo _receiverInfo = new ReceiverInfoBuilder().Build();
    private DeliveryAddress _deliveryAddress = new DeliveryAddressBuilder().Build();
    private Money _shippingCost = Money.Create(50m, "IRT");
    private Money _discountAmount = Money.Create(0m, "IRT");
    private DiscountCodeId? _appliedDiscountCodeId;
    private List<OrderItemSnapshot> _itemSnapshots = new() { new OrderItemSnapshotBuilder().Build() };
    private Guid _idempotencyKey = Guid.NewGuid();
    private DateOnly _orderDate = new(2026, 8, 4);
    private PaymentMethodId? _paymentMethodId;

    public OrderBuilder WithOrderId(OrderId id)
    {
        _orderId = id;
        return this;
    }

    public OrderBuilder WithUserId(UserId id)
    {
        _userId = id;
        return this;
    }

    public OrderBuilder WithReceiverInfo(ReceiverInfo info)
    {
        _receiverInfo = info;
        return this;
    }

    public OrderBuilder WithDeliveryAddress(DeliveryAddress addr)
    {
        _deliveryAddress = addr;
        return this;
    }

    public OrderBuilder WithShippingCost(Money cost)
    {
        _shippingCost = cost;
        return this;
    }

    public OrderBuilder WithShippingCost(decimal amount, string currency = "IRT")
    {
        _shippingCost = Money.Create(amount, currency);
        return this;
    }

    public OrderBuilder WithDiscountAmount(Money amount)
    {
        _discountAmount = amount;
        return this;
    }

    public OrderBuilder WithDiscountAmount(decimal amount, string currency = "IRT")
    {
        _discountAmount = Money.Create(amount, currency);
        return this;
    }

    public OrderBuilder WithAppliedDiscountCodeId(DiscountCodeId? id)
    {
        _appliedDiscountCodeId = id;
        return this;
    }

    public OrderBuilder WithItemSnapshots(params OrderItemSnapshot[] snapshots)
    {
        _itemSnapshots = new List<OrderItemSnapshot>(snapshots);
        return this;
    }

    public OrderBuilder WithNoItems()
    {
        _itemSnapshots = new List<OrderItemSnapshot>();
        return this;
    }

    public OrderBuilder WithIdempotencyKey(Guid key)
    {
        _idempotencyKey = key;
        return this;
    }

    public OrderBuilder WithOrderDate(DateOnly date)
    {
        _orderDate = date;
        return this;
    }

    public OrderBuilder WithPaymentMethodId(PaymentMethodId? id)
    {
        _paymentMethodId = id;
        return this;
    }

    public Order Build() =>
        Order.Place(
            _orderId, _userId, _receiverInfo, _deliveryAddress,
            _shippingCost, _discountAmount, _appliedDiscountCodeId,
            _itemSnapshots, _idempotencyKey, _orderDate, _paymentMethodId);
}
