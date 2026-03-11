using Domain.Common.ValueObjects;

namespace Tests.DomainTest.Discount;

public class DiscountCodeUsageTests
{
    [Fact]
    public void IncrementUsage_ShouldIncreaseUsedCount()
    {
        var discount = new DiscountCodeBuilder().Build();
        var initialCount = discount.UsageCount;

        discount.IncrementUsage();

        discount.UsageCount.Should().Be(initialCount + 1);
    }

    [Fact]
    public void IncrementUsage_WhenDeletedDiscount_ShouldThrowDomainException()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Delete();

        var act = () => discount.IncrementUsage();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IncrementUsage_WhenUsageLimitReached_ShouldThrowException()
    {
        var discount = new DiscountCodeBuilder()
            .WithUsageLimit(2)
            .Build();

        discount.IncrementUsage();
        discount.IncrementUsage();

        var act = () => discount.IncrementUsage();

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void HasReachedUsageLimit_WhenBelowLimit_ShouldReturnFalse()
    {
        var discount = new DiscountCodeBuilder()
            .WithUsageLimit(10)
            .Build();

        discount.HasReachedUsageLimit().Should().BeFalse();
    }

    [Fact]
    public void HasReachedUsageLimit_WhenAtLimit_ShouldReturnTrue()
    {
        var discount = new DiscountCodeBuilder()
            .WithUsageLimit(1)
            .Build();

        discount.IncrementUsage();

        discount.HasReachedUsageLimit().Should().BeTrue();
    }

    [Fact]
    public void HasReachedUsageLimit_WhenNoLimit_ShouldReturnFalse()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.HasReachedUsageLimit().Should().BeFalse();
    }

    [Fact]
    public void RemainingUsage_WhenNoLimit_ShouldReturnMaxInt()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.RemainingUsage().Should().Be(int.MaxValue);
    }

    [Fact]
    public void RemainingUsage_WhenLimitSet_ShouldReturnCorrectValue()
    {
        var discount = new DiscountCodeBuilder()
            .WithUsageLimit(10)
            .Build();

        discount.IncrementUsage();
        discount.IncrementUsage();

        discount.RemainingUsage().Should().Be(8);
    }

    [Fact]
    public void RemainingUsage_WhenLimitReached_ShouldReturnZero()
    {
        var discount = new DiscountCodeBuilder()
            .WithUsageLimit(2)
            .Build();

        discount.IncrementUsage();
        discount.IncrementUsage();

        discount.RemainingUsage().Should().Be(0);
    }

    [Fact]
    public void RecordUsage_ShouldAddUsageToCollection()
    {
        var discount = new DiscountCodeBuilder().Build();
        var discountAmount = Money.FromDecimal(100);

        discount.RecordUsage(userId: 1, orderId: 42, discountAmount);

        discount.Usages.Should().HaveCount(1);
    }

    [Fact]
    public void RecordUsage_ShouldIncrementUsedCount()
    {
        var discount = new DiscountCodeBuilder().Build();
        var discountAmount = Money.FromDecimal(100);

        discount.RecordUsage(userId: 1, orderId: 42, discountAmount);

        discount.UsageCount.Should().Be(1);
    }

    [Fact]
    public void RecordUsage_ShouldRaiseDomainEvent()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.ClearDomainEvents();
        var discountAmount = Money.FromDecimal(100);

        discount.RecordUsage(userId: 1, orderId: 42, discountAmount);

        discount.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "DiscountAppliedEvent");
    }

    [Fact]
    public void CancelUsage_WhenUsageExists_ShouldDecrementCount()
    {
        var discount = new DiscountCodeBuilder().Build();
        var discountAmount = Money.FromDecimal(100);
        var usage = discount.RecordUsage(userId: 1, orderId: 42, discountAmount);

        discount.CancelUsage(orderId: 42);

        discount.UsageCount.Should().Be(0);
    }

    [Fact]
    public void HasReachedUserUsageLimit_WhenBelowLimit_ShouldReturnFalse()
    {
        var discount = new DiscountCodeBuilder()
            .WithMaxUsagePerUser(3)
            .Build();

        discount.HasReachedUserUsageLimit(2).Should().BeFalse();
    }

    [Fact]
    public void HasReachedUserUsageLimit_WhenAtLimit_ShouldReturnTrue()
    {
        var discount = new DiscountCodeBuilder()
            .WithMaxUsagePerUser(3)
            .Build();

        discount.HasReachedUserUsageLimit(3).Should().BeTrue();
    }
}