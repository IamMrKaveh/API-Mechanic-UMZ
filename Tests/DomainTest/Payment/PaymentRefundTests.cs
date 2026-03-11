namespace Tests.DomainTest.Payment;

public class PaymentRefundTests
{
    [Fact]
    public void Refund_WhenSuccessful_ShouldSetRefundedStatus()
    {
        var tx = new PaymentTransactionBuilder().BuildSucceeded();

        tx.Refund("بازگشت وجه درخواست مشتری");

        tx.IsRefunded().Should().BeTrue();
    }

    [Fact]
    public void Refund_WhenPending_ShouldThrowDomainException()
    {
        var tx = new PaymentTransactionBuilder().Build();

        var act = () => tx.Refund();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Refund_WhenFailed_ShouldThrowDomainException()
    {
        var tx = new PaymentTransactionBuilder().BuildFailed();

        var act = () => tx.Refund();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Refund_ShouldRaisePaymentRefundedEvent()
    {
        var tx = new PaymentTransactionBuilder().BuildSucceeded();
        tx.ClearDomainEvents();

        tx.Refund();

        tx.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "PaymentRefundedEvent");
    }

    [Fact]
    public void Refund_WithNoReason_ShouldSetDefaultMessage()
    {
        var tx = new PaymentTransactionBuilder().BuildSucceeded();

        tx.Refund();

        tx.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Refund_WhenCancelled_ShouldThrowDomainException()
    {
        var tx = new PaymentTransactionBuilder().Build();
        tx.Cancel();

        var act = () => tx.Refund();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Delete_ShouldMarkAsDeleted()
    {
        var tx = new PaymentTransactionBuilder().Build();

        tx.Delete(deletedBy: 1);

        tx.IsDeleted.Should().BeTrue();
        tx.DeletedBy.Should().Be(1);
        tx.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ShouldNotThrow()
    {
        var tx = new PaymentTransactionBuilder().Build();
        tx.Delete();

        var act = () => tx.Delete();

        act.Should().NotThrow();
    }

    [Fact]
    public void MatchesAmount_WhenAmountMatches_ShouldReturnTrue()
    {
        var tx = new PaymentTransactionBuilder().WithAmount(500_000m).Build();

        tx.MatchesAmount(500_000m).Should().BeTrue();
    }

    [Fact]
    public void MatchesAmount_WhenAmountDiffers_ShouldReturnFalse()
    {
        var tx = new PaymentTransactionBuilder().WithAmount(500_000m).Build();

        tx.MatchesAmount(600_000m).Should().BeFalse();
    }

    [Fact]
    public void HasRefId_WhenSucceeded_ShouldReturnTrue()
    {
        var tx = new PaymentTransactionBuilder().BuildSucceeded(refId: 777777L);

        tx.HasRefId().Should().BeTrue();
        tx.RefId.Should().Be(777777L);
    }

    [Fact]
    public void HasRefId_WhenPending_ShouldReturnFalse()
    {
        var tx = new PaymentTransactionBuilder().Build();

        tx.HasRefId().Should().BeFalse();
    }

    [Fact]
    public void CanBeVerified_WhenPending_ShouldReturnTrue()
    {
        var tx = new PaymentTransactionBuilder().Build();

        tx.CanBeVerified().Should().BeTrue();
    }

    [Fact]
    public void CanBeVerified_WhenSucceeded_ShouldReturnFalse()
    {
        var tx = new PaymentTransactionBuilder().BuildSucceeded();

        tx.CanBeVerified().Should().BeFalse();
    }

    [Fact]
    public void GetTimeUntilExpiry_WhenPending_ShouldReturnPositiveValue()
    {
        var tx = new PaymentTransactionBuilder().Build();

        var timeUntilExpiry = tx.GetTimeUntilExpiry();

        timeUntilExpiry.Should().NotBeNull();
        timeUntilExpiry!.Value.TotalSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetTimeUntilExpiry_WhenSucceeded_ShouldReturnNull()
    {
        var tx = new PaymentTransactionBuilder().BuildSucceeded();

        var result = tx.GetTimeUntilExpiry();

        result.Should().BeNull();
    }
}