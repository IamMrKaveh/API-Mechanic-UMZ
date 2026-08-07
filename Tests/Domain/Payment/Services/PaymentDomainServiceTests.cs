using Domain.Payment.Aggregates;
using Domain.Payment.Services;
using Domain.Payment.ValueObjects;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Payment.Services;

public class PaymentDomainServiceTests
{
    private static PaymentTransaction BuildAlreadyExpiredPending() =>
        new PaymentTransactionBuilder()
            .WithNow(DateTime.UtcNow.AddHours(-2))
            .WithExpiryMinutes(1)
            .Build();

    private static PaymentTransaction BuildFreshPending() =>
        new PaymentTransactionBuilder()
            .WithNow(DateTime.UtcNow)
            .WithExpiryMinutes(20)
            .Build();

    [Fact]
    public void ExpireStaleTransactions_WithEmptyCollection_ReturnsZero()
    {
        PaymentDomainService.ExpireStaleTransactions(Array.Empty<PaymentTransaction>()).ShouldBe(0);
    }

    [Fact]
    public void ExpireStaleTransactions_WithNullCollection_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            PaymentDomainService.ExpireStaleTransactions(null!));
    }

    [Fact]
    public void ExpireStaleTransactions_WithNoExpiredTransactions_ReturnsZeroAndLeavesStatusUntouched()
    {
        var fresh = BuildFreshPending();

        var count = PaymentDomainService.ExpireStaleTransactions(new[] { fresh });

        count.ShouldBe(0);
        fresh.Status.ShouldBe(PaymentStatus.Pending);
    }

    [Fact]
    public void ExpireStaleTransactions_WithExpiredPendingTransactions_ExpiresThemAndReturnsCount()
    {
        var expired1 = BuildAlreadyExpiredPending();
        var expired2 = BuildAlreadyExpiredPending();
        var fresh = BuildFreshPending();

        var count = PaymentDomainService.ExpireStaleTransactions(new[] { expired1, expired2, fresh });

        count.ShouldBe(2);
        expired1.Status.ShouldBe(PaymentStatus.Expired);
        expired2.Status.ShouldBe(PaymentStatus.Expired);
        fresh.Status.ShouldBe(PaymentStatus.Pending);
    }

    [Fact]
    public void ExpireStaleTransactions_IgnoresAlreadyTerminalTransactions()
    {
        var successful = new PaymentTransactionBuilder().Build();
        successful.MarkAsSuccess(refId: 1, DateTime.UtcNow);

        var failed = new PaymentTransactionBuilder().Build();
        failed.MarkAsFailed(DateTime.UtcNow);

        var count = PaymentDomainService.ExpireStaleTransactions(new[] { successful, failed });

        count.ShouldBe(0);
        successful.Status.ShouldBe(PaymentStatus.Success);
        failed.Status.ShouldBe(PaymentStatus.Failed);
    }
}
