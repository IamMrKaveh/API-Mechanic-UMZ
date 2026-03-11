namespace Tests.DomainTest.Payment;

public class PaymentStateTransitionTests
{
    [Fact]
    public void MarkAsVerificationInProgress_WhenPending_ShouldSetIsVerificationInProgress()
    {
        var tx = new PaymentTransactionBuilder().Build();

        tx.MarkAsVerificationInProgress();

        tx.IsVerificationInProgress.Should().BeTrue();
    }

    [Fact]
    public void MarkAsVerificationInProgress_WhenAlreadyProcessing_ShouldThrowDomainException()
    {
        var tx = new PaymentTransactionBuilder().BuildProcessing();

        var act = () => tx.MarkAsVerificationInProgress();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkAsSuccess_WhenProcessing_ShouldSetSuccessStatus()
    {
        var tx = new PaymentTransactionBuilder().BuildProcessing();

        tx.MarkAsSuccess(999888777L);

        tx.IsSuccessful().Should().BeTrue();
        tx.RefId.Should().Be(999888777L);
    }

    [Fact]
    public void MarkAsSuccess_ShouldClearVerificationInProgress()
    {
        var tx = new PaymentTransactionBuilder().BuildProcessing();

        tx.MarkAsSuccess(123L);

        tx.IsVerificationInProgress.Should().BeFalse();
    }

    [Fact]
    public void MarkAsSuccess_WhenAlreadySucceeded_ShouldThrowException()
    {
        var tx = new PaymentTransactionBuilder().BuildSucceeded();

        var act = () => tx.MarkAsSuccess(111L);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void MarkAsSuccess_WhenFailed_ShouldThrowDomainException()
    {
        var tx = new PaymentTransactionBuilder().Build();
        tx.MarkAsVerificationInProgress();
        tx.MarkAsFailed();
        tx.ClearDomainEvents();

        var act = () => tx.MarkAsSuccess(555L);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkAsFailed_WhenProcessing_ShouldSetFailedStatus()
    {
        var tx = new PaymentTransactionBuilder().BuildProcessing();

        tx.MarkAsFailed("خطا در اتصال");

        tx.IsFailed().Should().BeTrue();
        tx.ErrorMessage.Should().Be("خطا در اتصال");
    }

    [Fact]
    public void MarkAsFailed_WhenSucceeded_ShouldThrowDomainException()
    {
        var tx = new PaymentTransactionBuilder().BuildSucceeded();

        var act = () => tx.MarkAsFailed();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_WhenPending_ShouldSetCancelledStatus()
    {
        var tx = new PaymentTransactionBuilder().Build();

        tx.Cancel("انصراف کاربر");

        tx.IsCancelled().Should().BeTrue();
        tx.ErrorMessage.Should().Be("انصراف کاربر");
    }

    [Fact]
    public void Cancel_WithNoReason_ShouldSetDefaultMessage()
    {
        var tx = new PaymentTransactionBuilder().Build();

        tx.Cancel();

        tx.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Cancel_WhenSucceeded_ShouldThrowDomainException()
    {
        var tx = new PaymentTransactionBuilder().BuildSucceeded();

        var act = () => tx.Cancel();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Expire_WhenPending_ShouldSetExpiredStatus()
    {
        var tx = new PaymentTransactionBuilder().Build();

        tx.Expire();

        tx.Status.Should().Be(PaymentStatus.Expired);
    }

    [Fact]
    public void Expire_WhenSucceeded_ShouldNotChangeStatus()
    {
        var tx = new PaymentTransactionBuilder().BuildSucceeded();

        tx.Expire();

        tx.IsSuccessful().Should().BeTrue();
    }

    [Fact]
    public void Expire_WhenFailed_ShouldNotChangeStatus()
    {
        var tx = new PaymentTransactionBuilder().BuildFailed();

        tx.Expire();

        tx.IsFailed().Should().BeTrue();
    }

    [Fact]
    public void MarkAsSuccess_ShouldSetVerifiedAt()
    {
        var tx = new PaymentTransactionBuilder().BuildProcessing();

        tx.MarkAsSuccess(1234L);

        tx.VerifiedAt.Should().NotBeNull();
        tx.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsSuccess_WithCardDetails_ShouldPersistCardDetails()
    {
        var tx = new PaymentTransactionBuilder().BuildProcessing();

        tx.MarkAsSuccess(1234L, cardPan: "6037-****-****-1234", cardHash: "abc123hash");

        tx.CardPan.Should().Be("6037-****-****-1234");
        tx.CardHash.Should().Be("abc123hash");
    }
}