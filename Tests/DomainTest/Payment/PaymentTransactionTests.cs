using Domain.Payment.Aggregates;

namespace Tests.DomainTest.Payment;

public class PaymentTransactionTests
{
    private static PaymentTransaction CreatePendingTransaction(
        int orderId = 1,
        int userId = 1,
        string authority = "AUTH123",
        decimal amount = 1000m,
        string gateway = "Zarinpal")
    {
        return PaymentTransaction.Initiate(orderId, userId, authority, amount, gateway);
    }

    [Fact]
    public void Initiate_WithValidParameters_ShouldReturnPendingTransaction()
    {
        var transaction = CreatePendingTransaction();

        transaction.Should().NotBeNull();
        transaction.Status.Should().Be(Domain.Payment.ValueObjects.PaymentStatus.Pending);
        transaction.IsDeleted.Should().BeFalse();
        transaction.IsVerificationInProgress.Should().BeFalse();
    }

    [Fact]
    public void Initiate_ShouldSetExpiresAt()
    {
        var transaction = CreatePendingTransaction();

        transaction.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Initiate_ShouldRaisePaymentInitiatedEvent()
    {
        var transaction = CreatePendingTransaction();

        transaction.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "PaymentInitiatedEvent");
    }

    [Fact]
    public void Initiate_ShouldSetCreatedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var transaction = CreatePendingTransaction();

        transaction.CreatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void Initiate_WithNegativeAmount_ShouldThrowException()
    {
        var act = () => PaymentTransaction.Initiate(1, 1, "AUTH", -100, "Zarinpal");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Initiate_WithZeroAmount_ShouldThrowException()
    {
        var act = () => PaymentTransaction.Initiate(1, 1, "AUTH", 0, "Zarinpal");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void MarkAsVerificationInProgress_WhenPending_ShouldSetProcessingStatus()
    {
        var transaction = CreatePendingTransaction();

        transaction.MarkAsVerificationInProgress();

        transaction.IsVerificationInProgress.Should().BeTrue();
        transaction.Status.Should().Be(Domain.Payment.ValueObjects.PaymentStatus.Processing);
    }

    [Fact]
    public void MarkAsSuccess_ShouldSetSuccessStatus()
    {
        var transaction = CreatePendingTransaction();
        transaction.MarkAsVerificationInProgress();

        transaction.MarkAsSuccess(refId: 123456789L);

        transaction.Status.Should().Be(Domain.Payment.ValueObjects.PaymentStatus.Success);
        transaction.RefId.Should().Be(123456789L);
        transaction.VerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsSuccess_ShouldSetCardDetails()
    {
        var transaction = CreatePendingTransaction();
        transaction.MarkAsVerificationInProgress();

        transaction.MarkAsSuccess(123456789L, cardPan: "1234-****-****-5678", cardHash: "hash123");

        transaction.CardPan.Should().Be("1234-****-****-5678");
        transaction.CardHash.Should().Be("hash123");
    }

    [Fact]
    public void MarkAsSuccess_ShouldRaisePaymentSucceededEvent()
    {
        var transaction = CreatePendingTransaction();
        transaction.MarkAsVerificationInProgress();
        transaction.ClearDomainEvents();

        transaction.MarkAsSuccess(123456789L);

        transaction.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "PaymentSucceededEvent");
    }

    [Fact]
    public void MarkAsFailed_ShouldSetFailedStatus()
    {
        var transaction = CreatePendingTransaction();
        transaction.MarkAsVerificationInProgress();

        transaction.MarkAsFailed("خطای پرداخت");

        transaction.Status.Should().Be(Domain.Payment.ValueObjects.PaymentStatus.Failed);
        transaction.ErrorMessage.Should().Be("خطای پرداخت");
    }

    [Fact]
    public void MarkAsFailed_WithNoMessage_ShouldSetDefaultErrorMessage()
    {
        var transaction = CreatePendingTransaction();
        transaction.MarkAsVerificationInProgress();

        transaction.MarkAsFailed();

        transaction.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MarkAsFailed_ShouldRaisePaymentFailedEvent()
    {
        var transaction = CreatePendingTransaction();
        transaction.MarkAsVerificationInProgress();
        transaction.ClearDomainEvents();

        transaction.MarkAsFailed("خطا");

        transaction.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "PaymentFailedEvent");
    }

    [Fact]
    public void Expire_WhenPending_ShouldSetExpiredStatus()
    {
        var transaction = CreatePendingTransaction();

        transaction.Expire();

        transaction.Status.Should().Be(Domain.Payment.ValueObjects.PaymentStatus.Expired);
    }

    [Fact]
    public void Expire_ShouldRaisePaymentExpiredEvent()
    {
        var transaction = CreatePendingTransaction();
        transaction.ClearDomainEvents();

        transaction.Expire();

        transaction.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "PaymentExpiredEvent");
    }

    [Fact]
    public void Expire_WhenAlreadySucceeded_ShouldNotChangeStatus()
    {
        var transaction = CreatePendingTransaction();
        transaction.MarkAsVerificationInProgress();
        transaction.MarkAsSuccess(123456789L);

        transaction.Expire();

        transaction.Status.Should().Be(Domain.Payment.ValueObjects.PaymentStatus.Success);
    }

    [Fact]
    public void Cancel_WhenPending_ShouldSetCancelledStatus()
    {
        var transaction = CreatePendingTransaction();

        transaction.Cancel("لغو توسط کاربر");

        transaction.Status.Should().Be(Domain.Payment.ValueObjects.PaymentStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadySucceeded_ShouldThrowDomainException()
    {
        var transaction = CreatePendingTransaction();
        transaction.MarkAsVerificationInProgress();
        transaction.MarkAsSuccess(123456789L);

        var act = () => transaction.Cancel();

        act.Should().Throw<DomainException>();
    }
}