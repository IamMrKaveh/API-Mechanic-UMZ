using Application.Wallet.EventHandlers;
using MediatR;

namespace Tests.Application.Wallet.EventHandlers;

public sealed class WalletRefundApplicationEventTests
{
    [Fact]
    public void Constructor_WhenGivenValues_AssignsPositionalProperties()
    {
        var transactionId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var amount = 123_456m;

        var sut = new WalletRefundApplicationEvent(transactionId, orderId, amount);

        sut.TransactionId.ShouldBe(transactionId);
        sut.OrderId.ShouldBe(orderId);
        sut.Amount.ShouldBe(amount);
    }

    [Fact]
    public void Instance_ImplementsMediatRINotification()
    {
        var sut = new WalletRefundApplicationEvent(Guid.NewGuid(), Guid.NewGuid(), 1m);

        sut.ShouldBeAssignableTo<INotification>();
    }

    [Fact]
    public void RecordEquality_WhenAllFieldsMatch_TwoInstancesAreEqual()
    {
        var transactionId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var left = new WalletRefundApplicationEvent(transactionId, orderId, 500m);
        var right = new WalletRefundApplicationEvent(transactionId, orderId, 500m);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void RecordEquality_WhenAmountDiffers_InstancesAreNotEqual()
    {
        var transactionId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var left = new WalletRefundApplicationEvent(transactionId, orderId, 500m);
        var right = new WalletRefundApplicationEvent(transactionId, orderId, 600m);

        left.ShouldNotBe(right);
    }

    [Fact]
    public void RecordEquality_WhenIdsDiffer_InstancesAreNotEqual()
    {
        var amount = 100m;
        var left = new WalletRefundApplicationEvent(Guid.NewGuid(), Guid.NewGuid(), amount);
        var right = new WalletRefundApplicationEvent(Guid.NewGuid(), Guid.NewGuid(), amount);

        left.ShouldNotBe(right);
    }
}

