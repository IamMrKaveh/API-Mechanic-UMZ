namespace Tests.DomainTest.Discount;

public class DiscountCodeExpiryTests
{
    [Fact]
    public void MarkAsExpired_WhenCurrentlyValid_ShouldDeactivateDiscount()
    {
        var discount = new DiscountCodeBuilder().Build();

        var now = DateTime.UtcNow;

        discount.MarkAsExpired(now);

        discount.IsActive.Should().BeFalse();
    }

    [Fact]
    public void MarkAsExpired_WhenCurrentlyValid_ShouldRaiseDomainEvent()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.ClearDomainEvents();

        var now = DateTime.UtcNow;

        discount.MarkAsExpired(now);

        discount.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "DiscountExpiredEvent");
    }

    [Fact]
    public void MarkAsExpired_WhenAlreadyInactive_ShouldNotChangeState()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Deactivate();
        discount.ClearDomainEvents();

        var now = DateTime.UtcNow;

        discount.MarkAsExpired(now);

        discount.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MarkAsExpired_WhenDeleted_ShouldNotChangeState()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Delete();
        discount.ClearDomainEvents();

        var now = DateTime.UtcNow;

        discount.MarkAsExpired(now);

        discount.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void TimeUntilExpiry_WhenExpiresInFuture_ShouldReturnPositiveTimespan()
    {
        var discount = new DiscountCodeBuilder()
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build();

        var now = DateTime.UtcNow;

        var remaining = discount.TimeUntilExpiry(now);

        remaining.Should().NotBeNull();
        remaining!.Value.TotalSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TimeUntilExpiry_WhenNoExpiresAt_ShouldReturnNull()
    {
        var discount = new DiscountCodeBuilder().Build();

        var now = DateTime.UtcNow;

        var remaining = discount.TimeUntilExpiry(now);

        remaining.Should().BeNull();
    }

    [Fact]
    public void TimeUntilStart_WhenNotYetStarted_ShouldReturnPositiveTimespan()
    {
        var discount = new DiscountCodeBuilder()
            .NotYetStarted()
            .Build();

        var now = DateTime.UtcNow;

        var remaining = discount.TimeUntilStart(now);

        remaining.Should().NotBeNull();
        remaining!.Value.TotalSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TimeUntilStart_WhenAlreadyStarted_ShouldReturnNull()
    {
        var discount = new DiscountCodeBuilder()
            .WithStartsAt(DateTime.UtcNow.AddDays(-1))
            .Build();

        var now = DateTime.UtcNow;

        var remaining = discount.TimeUntilStart(now);

        remaining.Should().BeNull();
    }

    [Fact]
    public void MeetsMinimumOrderAmount_WhenOrderExceedsMinimum_ShouldReturnTrue()
    {
        var discount = new DiscountCodeBuilder()
            .WithMinOrderAmount(500)
            .Build();

        discount.MeetsMinimumOrderAmount(1000).Should().BeTrue();
    }

    [Fact]
    public void MeetsMinimumOrderAmount_WhenOrderBelowMinimum_ShouldReturnFalse()
    {
        var discount = new DiscountCodeBuilder()
            .WithMinOrderAmount(2000)
            .Build();

        discount.MeetsMinimumOrderAmount(1000).Should().BeFalse();
    }

    [Fact]
    public void MeetsMinimumOrderAmount_WhenNoMinimum_ShouldReturnTrue()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.MeetsMinimumOrderAmount(1).Should().BeTrue();
    }
}