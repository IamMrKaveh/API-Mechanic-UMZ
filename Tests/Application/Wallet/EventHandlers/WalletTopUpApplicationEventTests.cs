using Application.Wallet.EventHandlers;
using MediatR;

namespace Tests.Application.Wallet.EventHandlers;

public sealed class WalletTopUpApplicationEventTests
{
    [Fact]
    public void Constructor_WhenGivenValues_AssignsPositionalProperties()
    {
        var userId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var sut = new WalletTopUpApplicationEvent(userId, transactionId, orderId);

        sut.UserId.ShouldBe(userId);
        sut.TransactionId.ShouldBe(transactionId);
        sut.OrderId.ShouldBe(orderId);
    }

    [Fact]
    public void Instance_ImplementsMediatRINotification()
    {
        var sut = new WalletTopUpApplicationEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        sut.ShouldBeAssignableTo<INotification>();
    }

    [Fact]
    public void RecordEquality_WhenAllFieldsMatch_TwoInstancesAreEqual()
    {
        var userId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var left = new WalletTopUpApplicationEvent(userId, transactionId, orderId);
        var right = new WalletTopUpApplicationEvent(userId, transactionId, orderId);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void RecordEquality_WhenUserIdDiffers_InstancesAreNotEqual()
    {
        var transactionId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var left = new WalletTopUpApplicationEvent(Guid.NewGuid(), transactionId, orderId);
        var right = new WalletTopUpApplicationEvent(Guid.NewGuid(), transactionId, orderId);

        left.ShouldNotBe(right);
    }
}

