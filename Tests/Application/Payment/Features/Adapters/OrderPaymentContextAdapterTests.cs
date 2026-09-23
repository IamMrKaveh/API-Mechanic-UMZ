using Application.Payment.Features.Adapters;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Application.Payment.Features.Adapters;

public class OrderPaymentContextAdapterTests
{
    [Fact]
    public void Adapter_ExposesOrderIdentityAndStatus()
    {
        var order = new OrderBuilder().Build();

        var adapter = new OrderPaymentContextAdapter(order);

        adapter.Id.ShouldBe(order.Id);
        adapter.IsPaid.ShouldBe(order.IsPaid);
        adapter.IsDelivered.ShouldBe(order.IsDelivered);
        adapter.StatusDisplayName.ShouldBe(order.Status.DisplayName);
    }

    [Fact]
    public void MarkAsPaid_DelegatesToOrder()
    {
        var order = new OrderBuilder().Build();
        var adapter = new OrderPaymentContextAdapter(order);
        var transactionId = PaymentTransactionId.NewId();

        adapter.MarkAsPaid(transactionId);

        order.IsPaid.ShouldBeTrue();
    }

    [Fact]
    public void StartProcessing_DelegatesToOrder()
    {
        var order = new OrderBuilder().Build();
        var adapter = new OrderPaymentContextAdapter(order);
        adapter.MarkAsPaid(PaymentTransactionId.NewId());

        adapter.StartProcessing();

        order.Status.ShouldBe(OrderStatusValue.Processing);
    }

    [Fact]
    public void Refund_DelegatesToOrder()
    {
        var order = new OrderBuilder().Build();
        var adapter = new OrderPaymentContextAdapter(order);
        adapter.MarkAsPaid(PaymentTransactionId.NewId());

        adapter.Refund();

        order.Status.ShouldBe(OrderStatusValue.Refunded);
    }

    [Fact]
    public void Adapter_IdMatchesOrderIdValue()
    {
        var orderId = OrderId.NewId();
        var order = new OrderBuilder().WithOrderId(orderId).Build();

        var adapter = new OrderPaymentContextAdapter(order);

        adapter.Id.Value.ShouldBe(orderId.Value);
    }
}
