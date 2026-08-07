using Domain.Order.Events;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Domain.Order.Events;

public class OrderEventsTests
{
    [Fact]
    public void OrderCreatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        var orderNumber = OrderNumber.Generate(new DateOnly(2026, 8, 4));
        var idempotencyKey = Guid.NewGuid();

        var sut = new OrderCreatedEvent(orderId, userId, orderNumber, 500m, "IRT", 3, idempotencyKey);

        sut.OrderId.ShouldBe(orderId);
        sut.UserId.ShouldBe(userId);
        sut.OrderNumber.ShouldBe(orderNumber);
        sut.FinalAmount.ShouldBe(500m);
        sut.Currency.ShouldBe("IRT");
        sut.ItemsCount.ShouldBe(3);
        sut.IdempotencyKey.ShouldBe(idempotencyKey);
    }

    [Fact]
    public void OrderPaidEvent_ExposesConstructorArgumentsAsProperties()
    {
        var orderId = OrderId.NewId();
        var orderNumber = OrderNumber.Create("ORD-1");
        var userId = UserId.NewId();
        var paymentTxId = PaymentTransactionId.NewId();

        var sut = new OrderPaidEvent(orderId, orderNumber, userId, paymentTxId, 500m, "IRT");

        sut.OrderId.ShouldBe(orderId);
        sut.OrderNumber.ShouldBe(orderNumber);
        sut.UserId.ShouldBe(userId);
        sut.PaymentTransactionId.ShouldBe(paymentTxId);
        sut.PaidAmount.ShouldBe(500m);
        sut.Currency.ShouldBe("IRT");
    }

    [Fact]
    public void OrderCancelledEvent_ExposesConstructorArgumentsAsProperties()
    {
        var orderId = OrderId.NewId();
        var orderNumber = OrderNumber.Create("ORD-1");
        var userId = UserId.NewId();

        var sut = new OrderCancelledEvent(orderId, orderNumber, userId, "changed mind", true);

        sut.OrderId.ShouldBe(orderId);
        sut.OrderNumber.ShouldBe(orderNumber);
        sut.UserId.ShouldBe(userId);
        sut.CancellationReason.ShouldBe("changed mind");
        sut.WasPaid.ShouldBeTrue();
    }

    [Fact]
    public void OrderStatusChangedEvent_ExposesPreviousAndNewStatuses()
    {
        var orderId = OrderId.NewId();
        var orderNumber = OrderNumber.Create("ORD-1");
        var userId = UserId.NewId();

        var sut = new OrderStatusChangedEvent(
            orderId, orderNumber, userId, OrderStatusValue.Created, OrderStatusValue.Paid);

        sut.OrderId.ShouldBe(orderId);
        sut.OrderNumber.ShouldBe(orderNumber);
        sut.UserId.ShouldBe(userId);
        sut.PreviousStatus.ShouldBe(OrderStatusValue.Created);
        sut.NewStatus.ShouldBe(OrderStatusValue.Paid);
    }

    [Fact]
    public void OrderExpiredEvent_ExposesOrderId()
    {
        var orderId = OrderId.NewId();

        new OrderExpiredEvent(orderId).OrderId.ShouldBe(orderId);
    }
}
