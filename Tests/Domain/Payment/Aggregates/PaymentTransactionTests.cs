using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.Events;
using Domain.Payment.Exceptions;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Payment.Aggregates;

public class PaymentTransactionTests
{
    [Fact]
    public void Initiate_WithValidInput_ReturnsPendingTransactionWithVersionOne()
    {
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        var now = new DateTime(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);

        var sut = new PaymentTransactionBuilder()
            .WithOrderId(orderId)
            .WithUserId(userId)
            .WithAuthority("AUTH-12345")
            .WithAmount(50_000m)
            .WithGateway("Zarinpal")
            .WithNow(now)
            .WithDescription("test tx")
            .WithExpiryMinutes(20)
            .Build();

        sut.Id.ShouldNotBeNull();
        sut.OrderId.ShouldBe(orderId);
        sut.UserId.ShouldBe(userId);
        sut.Authority.Value.ShouldBe("AUTH-12345");
        sut.Amount.Amount.ShouldBe(50_000m);
        sut.Gateway.Value.ShouldBe("Zarinpal");
        sut.Description.ShouldBe("test tx");
        sut.ExpiresAt.ShouldBe(now.AddMinutes(20));
        sut.CreatedAt.ShouldBe(now);
        sut.UpdatedAt.ShouldBeNull();
        sut.Status.ShouldBe(PaymentStatus.Pending);
        sut.IsVerificationInProgress.ShouldBeFalse();
        sut.RefId.ShouldBeNull();
        sut.Fee.ShouldBe(0m);
        sut.Version.ShouldBe(1);
    }

    [Fact]
    public void Initiate_RaisesExactlyOnePaymentInitiatedEvent()
    {
        var orderId = OrderId.NewId();

        var sut = new PaymentTransactionBuilder()
            .WithOrderId(orderId)
            .WithAmount(75_000m)
            .Build();

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<PaymentInitiatedEvent>();
        evt.PaymentTransactionId.ShouldBe(sut.Id);
        evt.OrderId.ShouldBe(orderId);
        evt.Amount.ShouldBe(75_000m);
    }

    [Fact]
    public void Initiate_WithNullOrderId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new PaymentTransactionBuilder().WithOrderId(null!).Build());
    }

    [Fact]
    public void Initiate_WithNullUserId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new PaymentTransactionBuilder().WithUserId(null!).Build());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.5)]
    public void Initiate_WithNonPositiveAmount_ThrowsInvalidPaymentAmountException(double amount)
    {
        Should.Throw<InvalidPaymentAmountException>(() =>
            new PaymentTransactionBuilder().WithAmount((decimal)amount).Build());
    }

    [Fact]
    public void Initiate_WithDescriptionExceeding500Chars_ThrowsDomainException()
    {
        var description = new string('a', 501);

        Should.Throw<DomainException>(() =>
            new PaymentTransactionBuilder().WithDescription(description).Build());
    }

    [Fact]
    public void Initiate_WithDescriptionExactly500Chars_Succeeds()
    {
        var description = new string('a', 500);

        var sut = new PaymentTransactionBuilder().WithDescription(description).Build();

        sut.Description.ShouldBe(description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Initiate_WithNonPositiveExpiryMinutes_ThrowsDomainException(int expiryMinutes)
    {
        Should.Throw<DomainException>(() =>
            new PaymentTransactionBuilder().WithExpiryMinutes(expiryMinutes).Build());
    }

    [Fact]
    public void Initiate_WithExpiryMinutesOver60_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            new PaymentTransactionBuilder().WithExpiryMinutes(61).Build());
    }

    [Fact]
    public void Initiate_WithExpiryMinutesExactly60_Succeeds()
    {
        var sut = new PaymentTransactionBuilder().WithExpiryMinutes(60).Build();

        (sut.ExpiresAt - sut.CreatedAt).TotalMinutes.ShouldBe(60);
    }

    [Fact]
    public void InitiateWithId_WithSuppliedId_UsesThatId()
    {
        var id = PaymentTransactionId.NewId();
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        var now = DateTime.UtcNow;

        var sut = PaymentTransaction.InitiateWithId(
            id, orderId, userId, "AUTH-99999", 10_000m, "Zarinpal", now);

        sut.Id.ShouldBe(id);
        sut.OrderId.ShouldBe(orderId);
        sut.UserId.ShouldBe(userId);
    }

    [Fact]
    public void InitiateWithId_WithNullId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            PaymentTransaction.InitiateWithId(
                null!, OrderId.NewId(), UserId.NewId(), "AUTH-12345",
                10_000m, "Zarinpal", DateTime.UtcNow));
    }

    [Fact]
    public void MarkAsSuccess_FromPending_TransitionsToSuccessAndSetsVerifiedAt()
    {
        var sut = new PaymentTransactionBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;
        var now = DateTime.UtcNow;

        sut.MarkAsSuccess(refId: 12345, now, fee: 250m);

        sut.Status.ShouldBe(PaymentStatus.Success);
        sut.RefId.ShouldBe(12345);
        sut.Fee.ShouldBe(250m);
        sut.VerifiedAt.ShouldBe(now);
        sut.UpdatedAt.ShouldBe(now);
        sut.IsVerificationInProgress.ShouldBeFalse();
        sut.Version.ShouldBe(versionBefore + 1);
        sut.IsSuccessful().ShouldBeTrue();
    }

    [Fact]
    public void MarkAsSuccess_RaisesPaymentSucceededEventWithDeterministicFields()
    {
        var orderId = OrderId.NewId();
        var sut = new PaymentTransactionBuilder().WithOrderId(orderId).WithAmount(50_000m).Build();
        sut.ClearDomainEvents();

        sut.MarkAsSuccess(refId: 7777, DateTime.UtcNow);

        var evt = sut.DomainEvents.Single().ShouldBeOfType<PaymentSucceededEvent>();
        evt.PaymentTransactionId.ShouldBe(sut.Id);
        evt.OrderId.ShouldBe(orderId);
        evt.RefId.ShouldBe(7777L);
        evt.Amount.Amount.ShouldBe(50_000m);
        evt.UserId.ShouldNotBeNull();
    }

    [Fact]
    public void MarkAsSuccess_WhenAlreadySuccessful_ThrowsPaymentAlreadyVerifiedException()
    {
        var sut = new PaymentTransactionBuilder().Build();
        sut.MarkAsSuccess(refId: 1, DateTime.UtcNow);

        Should.Throw<PaymentAlreadyVerifiedException>(() =>
            sut.MarkAsSuccess(refId: 2, DateTime.UtcNow));
    }

    [Fact]
    public void MarkAsSuccess_WhenFailed_ThrowsDomainException()
    {
        var sut = new PaymentTransactionBuilder().Build();
        sut.MarkAsFailed(DateTime.UtcNow);

        Should.Throw<DomainException>(() => sut.MarkAsSuccess(refId: 1, DateTime.UtcNow));
    }

    [Fact]
    public void MarkAsSuccess_WhenExpired_ThrowsPaymentExpiredException()
    {
        var pastNow = DateTime.UtcNow.AddHours(-2);
        var sut = new PaymentTransactionBuilder().WithNow(pastNow).WithExpiryMinutes(1).Build();
        sut.Expire(DateTime.UtcNow);

        Should.Throw<PaymentExpiredException>(() => sut.MarkAsSuccess(refId: 1, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MarkAsSuccess_WithNonPositiveRefId_ThrowsDomainException(long refId)
    {
        var sut = new PaymentTransactionBuilder().Build();

        Should.Throw<DomainException>(() => sut.MarkAsSuccess(refId, DateTime.UtcNow));
    }

    [Fact]
    public void MarkAsSuccess_WithNegativeFee_ThrowsDomainException()
    {
        var sut = new PaymentTransactionBuilder().Build();

        Should.Throw<DomainException>(() => sut.MarkAsSuccess(refId: 1, DateTime.UtcNow, fee: -0.01m));
    }

    [Fact]
    public void MarkAsFailed_FromPending_TransitionsToFailedAndStoresErrorMessage()
    {
        var sut = new PaymentTransactionBuilder().Build();
        sut.ClearDomainEvents();
        var now = DateTime.UtcNow;

        sut.MarkAsFailed(now, "gateway timeout");

        sut.Status.ShouldBe(PaymentStatus.Failed);
        sut.ErrorMessage.ShouldBe("gateway timeout");
        sut.UpdatedAt.ShouldBe(now);
        sut.IsVerificationInProgress.ShouldBeFalse();
        var evt = sut.DomainEvents.Single().ShouldBeOfType<PaymentFailedEvent>();
        evt.PaymentTransactionId.ShouldBe(sut.Id);
        evt.Reason.ShouldBe("gateway timeout");
    }

    [Fact]
    public void MarkAsFailed_WithNullErrorMessage_StoresDefaultErrorMessage()
    {
        var sut = new PaymentTransactionBuilder().Build();

        sut.MarkAsFailed(DateTime.UtcNow);

        sut.ErrorMessage.ShouldBe("خطای نامشخص");
    }

    [Fact]
    public void MarkAsFailed_WhenAlreadySuccessful_ThrowsDomainException()
    {
        var sut = new PaymentTransactionBuilder().Build();
        sut.MarkAsSuccess(refId: 1, DateTime.UtcNow);

        Should.Throw<DomainException>(() => sut.MarkAsFailed(DateTime.UtcNow));
    }

    [Fact]
    public void Expire_WhenPending_TransitionsToExpiredAndRaisesEvent()
    {
        var pastNow = DateTime.UtcNow.AddHours(-2);
        var sut = new PaymentTransactionBuilder()
            .WithNow(pastNow)
            .WithExpiryMinutes(1)
            .Build();
        sut.ClearDomainEvents();
        var now = DateTime.UtcNow;

        sut.Expire(now);

        sut.Status.ShouldBe(PaymentStatus.Expired);
        sut.UpdatedAt.ShouldBe(now);
        sut.IsVerificationInProgress.ShouldBeFalse();
        var evt = sut.DomainEvents.Single().ShouldBeOfType<PaymentExpiredEvent>();
        evt.PaymentTransactionId.ShouldBe(sut.Id);
    }

    [Fact]
    public void Expire_WhenAlreadySuccessful_IsNoOp()
    {
        var sut = new PaymentTransactionBuilder().Build();
        sut.MarkAsSuccess(refId: 1, DateTime.UtcNow);
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Expire(DateTime.UtcNow);

        sut.Status.ShouldBe(PaymentStatus.Success);
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Refund_FromSuccess_TransitionsToRefundedAndRaisesEvent()
    {
        var orderId = OrderId.NewId();
        var sut = new PaymentTransactionBuilder().WithOrderId(orderId).WithAmount(50_000m).Build();
        sut.MarkAsSuccess(refId: 12345, DateTime.UtcNow);
        sut.ClearDomainEvents();
        var now = DateTime.UtcNow;

        sut.Refund(now, "customer request");

        sut.Status.ShouldBe(PaymentStatus.Refunded);
        sut.ErrorMessage.ShouldBe("customer request");
        sut.UpdatedAt.ShouldBe(now);
        sut.IsRefunded().ShouldBeTrue();
        var evt = sut.DomainEvents.Single().ShouldBeOfType<PaymentRefundedEvent>();
        evt.PaymentTransactionId.ShouldBe(sut.Id);
        evt.OrderId.ShouldBe(orderId);
        evt.Amount.Amount.ShouldBe(50_000m);
        evt.Reason.ShouldBe("customer request");
        evt.UserId.ShouldNotBeNull();
    }

    [Fact]
    public void Refund_WithNullReason_StoresDefaultReason()
    {
        var sut = new PaymentTransactionBuilder().Build();
        sut.MarkAsSuccess(refId: 1, DateTime.UtcNow);

        sut.Refund(DateTime.UtcNow);

        sut.ErrorMessage.ShouldBe("بازگشت وجه");
    }

    [Fact]
    public void Refund_WhenPending_ThrowsDomainException()
    {
        var sut = new PaymentTransactionBuilder().Build();

        Should.Throw<DomainException>(() => sut.Refund(DateTime.UtcNow, "reason"));
    }

    [Fact]
    public void Refund_WhenFailed_ThrowsDomainException()
    {
        var sut = new PaymentTransactionBuilder().Build();
        sut.MarkAsFailed(DateTime.UtcNow);

        Should.Throw<DomainException>(() => sut.Refund(DateTime.UtcNow, "reason"));
    }

    [Fact]
    public void IsExpired_WhenPendingAndPastExpiresAt_ReturnsTrue()
    {
        var pastNow = DateTime.UtcNow.AddHours(-2);
        var sut = new PaymentTransactionBuilder().WithNow(pastNow).WithExpiryMinutes(1).Build();

        sut.IsExpired(DateTime.UtcNow).ShouldBeTrue();
    }

    [Fact]
    public void IsExpired_WhenPendingAndBeforeExpiresAt_ReturnsFalse()
    {
        var sut = new PaymentTransactionBuilder().Build();

        sut.IsExpired(DateTime.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void IsExpired_WhenSuccessful_ReturnsFalseEvenAfterExpiresAt()
    {
        var pastNow = DateTime.UtcNow.AddHours(-2);
        var sut = new PaymentTransactionBuilder().WithNow(pastNow).WithExpiryMinutes(1).Build();
        sut.MarkAsSuccess(refId: 1, DateTime.UtcNow);

        sut.IsExpired(DateTime.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void IsSuccessful_OnPending_ReturnsFalse()
    {
        new PaymentTransactionBuilder().Build().IsSuccessful().ShouldBeFalse();
    }

    [Fact]
    public void IsPending_AfterInitiate_ReturnsTrue()
    {
        new PaymentTransactionBuilder().Build().IsPending().ShouldBeTrue();
    }

    [Fact]
    public void IsRefunded_AfterRefund_ReturnsTrue()
    {
        var sut = new PaymentTransactionBuilder().Build();
        sut.MarkAsSuccess(refId: 1, DateTime.UtcNow);
        sut.Refund(DateTime.UtcNow, "reason");

        sut.IsRefunded().ShouldBeTrue();
    }

    [Fact]
    public void CanBeVerified_OnPendingAndNotExpired_ReturnsTrue()
    {
        new PaymentTransactionBuilder().Build().CanBeVerified(DateTime.UtcNow).ShouldBeTrue();
    }

    [Fact]
    public void CanBeVerified_OnExpiredPending_ReturnsFalse()
    {
        var pastNow = DateTime.UtcNow.AddHours(-2);
        var sut = new PaymentTransactionBuilder().WithNow(pastNow).WithExpiryMinutes(1).Build();

        sut.CanBeVerified(DateTime.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void CanBeVerified_OnSuccessful_ReturnsFalse()
    {
        var sut = new PaymentTransactionBuilder().Build();
        sut.MarkAsSuccess(refId: 1, DateTime.UtcNow);

        sut.CanBeVerified(DateTime.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void CanExpire_OnPending_ReturnsTrue()
    {
        new PaymentTransactionBuilder().Build().CanExpire().ShouldBeTrue();
    }

    [Fact]
    public void CanExpire_OnTerminalStatuses_ReturnsFalse()
    {
        var sut = new PaymentTransactionBuilder().Build();
        sut.MarkAsSuccess(refId: 1, DateTime.UtcNow);

        sut.CanExpire().ShouldBeFalse();
    }
}
