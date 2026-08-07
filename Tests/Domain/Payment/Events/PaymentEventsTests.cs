using Domain.Order.ValueObjects;
using Domain.Payment.Events;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Payment.Events;

public class PaymentEventsTests
{
    [Fact]
    public void PaymentInitiatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var paymentTxId = PaymentTransactionId.NewId();
        var orderId = OrderId.NewId();

        var sut = new PaymentInitiatedEvent(paymentTxId, orderId, 50_000m);

        sut.PaymentTransactionId.ShouldBe(paymentTxId);
        sut.OrderId.ShouldBe(orderId);
        sut.Amount.ShouldBe(50_000m);
    }

    [Fact]
    public void PaymentSucceededEvent_ExposesConstructorArgumentsAsProperties()
    {
        var paymentTxId = PaymentTransactionId.NewId();
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        var amount = Money.Create(50_000m, "IRT");

        var sut = new PaymentSucceededEvent(paymentTxId, orderId, refId: 12345, userId, amount);

        sut.PaymentTransactionId.ShouldBe(paymentTxId);
        sut.OrderId.ShouldBe(orderId);
        sut.RefId.ShouldBe(12345L);
        sut.UserId.ShouldBe(userId);
        sut.Amount.ShouldBe(amount);
    }

    [Fact]
    public void PaymentFailedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var paymentTxId = PaymentTransactionId.NewId();
        var orderId = OrderId.NewId();

        var sut = new PaymentFailedEvent(paymentTxId, orderId, "gateway timeout");

        sut.PaymentTransactionId.ShouldBe(paymentTxId);
        sut.OrderId.ShouldBe(orderId);
        sut.Reason.ShouldBe("gateway timeout");
    }

    [Fact]
    public void PaymentExpiredEvent_ExposesConstructorArgumentsAsProperties()
    {
        var paymentTxId = PaymentTransactionId.NewId();
        var orderId = OrderId.NewId();

        var sut = new PaymentExpiredEvent(paymentTxId, orderId, 50_000m, "AUTH-12345");

        sut.PaymentTransactionId.ShouldBe(paymentTxId);
        sut.OrderId.ShouldBe(orderId);
        sut.Amount.ShouldBe(50_000m);
        sut.Authority.ShouldBe("AUTH-12345");
    }

    [Fact]
    public void PaymentRefundedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var paymentTxId = PaymentTransactionId.NewId();
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        var amount = Money.Create(50_000m, "IRT");

        var sut = new PaymentRefundedEvent(paymentTxId, orderId, userId, amount, "customer request");

        sut.PaymentTransactionId.ShouldBe(paymentTxId);
        sut.OrderId.ShouldBe(orderId);
        sut.UserId.ShouldBe(userId);
        sut.Amount.ShouldBe(amount);
        sut.Reason.ShouldBe("customer request");
    }

    [Fact]
    public void PaymentRefundedEvent_WithNullReason_StoresNull()
    {
        var sut = new PaymentRefundedEvent(
            PaymentTransactionId.NewId(), OrderId.NewId(), UserId.NewId(),
            Money.Create(1m, "IRT"), null);

        sut.Reason.ShouldBeNull();
    }

    [Fact]
    public void PaymentMethodCreatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var paymentMethodId = PaymentMethodId.NewId();
        var name = PaymentMethodName.Create("Zarinpal");
        var code = PaymentMethodCode.Create("zarinpal");

        var sut = new PaymentMethodCreatedEvent(paymentMethodId, name, code);

        sut.PaymentMethodId.ShouldBe(paymentMethodId);
        sut.Name.ShouldBe(name);
        sut.Code.ShouldBe(code);
    }

    [Fact]
    public void PaymentMethodUpdatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var paymentMethodId = PaymentMethodId.NewId();
        var name = PaymentMethodName.Create("Zarinpal");

        var sut = new PaymentMethodUpdatedEvent(paymentMethodId, name);

        sut.PaymentMethodId.ShouldBe(paymentMethodId);
        sut.Name.ShouldBe(name);
    }

    [Fact]
    public void PaymentMethodActivatedEvent_ExposesPaymentMethodId()
    {
        var paymentMethodId = PaymentMethodId.NewId();

        new PaymentMethodActivatedEvent(paymentMethodId).PaymentMethodId.ShouldBe(paymentMethodId);
    }

    [Fact]
    public void PaymentMethodDeactivatedEvent_ExposesPaymentMethodId()
    {
        var paymentMethodId = PaymentMethodId.NewId();

        new PaymentMethodDeactivatedEvent(paymentMethodId).PaymentMethodId.ShouldBe(paymentMethodId);
    }

    [Fact]
    public void PaymentMethodDeletedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var paymentMethodId = PaymentMethodId.NewId();
        var deletedBy = UserId.NewId();

        var sut = new PaymentMethodDeletedEvent(paymentMethodId, deletedBy);

        sut.PaymentMethodId.ShouldBe(paymentMethodId);
        sut.DeletedBy.ShouldBe(deletedBy);
    }

    [Fact]
    public void PaymentMethodDeletedEvent_WithNullDeletedBy_StoresNull()
    {
        var sut = new PaymentMethodDeletedEvent(PaymentMethodId.NewId(), null);

        sut.DeletedBy.ShouldBeNull();
    }
}
