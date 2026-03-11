namespace Tests.DomainTest.Discount;

public class DiscountCodeUpdateTests
{
    [Fact]
    public void Update_WithValidParameters_ShouldUpdateProperties()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.Update(
            percentage: 20,
            maxDiscountAmount: 500,
            minOrderAmount: 1000,
            usageLimit: 100,
            isActive: true,
            expiresAt: DateTime.UtcNow.AddDays(30)
        );

        discount.Percentage.Should().Be(20);
        discount.MaxDiscountAmount.Should().Be(500);
        discount.MinOrderAmount.Should().Be(1000);
        discount.UsageLimit.Should().Be(100);
    }

    [Fact]
    public void Update_ShouldSetUpdatedAt()
    {
        var discount = new DiscountCodeBuilder().Build();
        var before = DateTime.UtcNow.AddSeconds(-1);

        discount.Update(20, null, null, null, true, null);

        discount.UpdatedAt.Should().NotBeNull();
        discount.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void Update_WhenDeletedDiscount_ShouldThrowDomainException()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Delete();

        var act = () => discount.Update(20, null, null, null, true, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_WithIsActiveFalse_ShouldDeactivateDiscount()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.Update(10, null, null, null, isActive: false, null);

        discount.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Update_WithIsActiveTrue_ShouldActivateDiscount()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Deactivate();

        discount.Update(10, null, null, null, isActive: true, null);

        discount.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldUpdateMaxUsagePerUser()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.Update(10, null, null, null, true, null, maxUsagePerUser: 5);

        discount.MaxUsagePerUser.Should().Be(5);
    }

    [Fact]
    public void Update_WithInvalidPercentage_ShouldThrowDomainException()
    {
        var discount = new DiscountCodeBuilder().Build();

        var act = () => discount.Update(200, null, null, null, true, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_WithStartsAtAndExpiresAt_WhenStartsAtAfterExpiresAt_ShouldThrowDomainException()
    {
        var discount = new DiscountCodeBuilder().Build();

        var act = () => discount.Update(
            10, null, null, null, true,
            expiresAt: DateTime.UtcNow.AddDays(1),
            startsAt: DateTime.UtcNow.AddDays(5)
        );

        act.Should().Throw<DomainException>();
    }
}