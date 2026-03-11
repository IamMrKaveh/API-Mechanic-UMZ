namespace Tests.DomainTest.Discount;

public class DiscountCodeCreationTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnActiveDiscount()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("SAVE10")
            .WithPercentage(10)
            .Build();

        discount.Should().NotBeNull();
        discount.IsActive.Should().BeTrue();
        discount.UsageCount.Should().Be(0);
        discount.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldSetCodeInUpperCase()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("save10")
            .Build();

        discount.Code.Value.Should().Be("SAVE10");
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var discount = new DiscountCodeBuilder().Build();

        discount.CreatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void Create_ShouldRaiseDomainEvent()
    {
        var discount = new DiscountCodeBuilder().Build();

        discount.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "DiscountCreatedEvent");
    }

    [Fact]
    public void Create_WithMaxDiscountAmount_ShouldSetCorrectly()
    {
        var discount = new DiscountCodeBuilder()
            .WithMaxDiscountAmount(500)
            .Build();

        discount.MaxDiscountAmount.Should().Be(500);
    }

    [Fact]
    public void Create_WithMinOrderAmount_ShouldSetCorrectly()
    {
        var discount = new DiscountCodeBuilder()
            .WithMinOrderAmount(2000)
            .Build();

        discount.MinOrderAmount.Should().Be(2000);
    }

    [Fact]
    public void Create_WithUsageLimit_ShouldSetCorrectly()
    {
        var discount = new DiscountCodeBuilder()
            .WithUsageLimit(100)
            .Build();

        discount.UsageLimit.Should().Be(100);
    }

    [Fact]
    public void Create_WithMaxUsagePerUser_ShouldSetCorrectly()
    {
        var discount = new DiscountCodeBuilder()
            .WithMaxUsagePerUser(3)
            .Build();

        discount.MaxUsagePerUser.Should().Be(3);
    }

    [Fact]
    public void Create_WithNegativeMaxDiscountAmount_ShouldThrowDomainException()
    {
        var act = () => new DiscountCodeBuilder()
            .WithMaxDiscountAmount(-100)
            .Build();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithZeroMaxDiscountAmount_ShouldThrowDomainException()
    {
        var act = () => new DiscountCodeBuilder()
            .WithMaxDiscountAmount(0)
            .Build();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithNegativeMinOrderAmount_ShouldThrowDomainException()
    {
        var act = () => new DiscountCodeBuilder()
            .WithMinOrderAmount(-100)
            .Build();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ExpiresAtBeforeStartsAt_ShouldThrowDomainException()
    {
        var act = () => new DiscountCodeBuilder()
            .WithStartsAt(DateTime.UtcNow.AddDays(5))
            .WithExpiresAt(DateTime.UtcNow.AddDays(1))
            .Build();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithNullOptionalParameters_ShouldSucceed()
    {
        var discount = DiscountCode.Create("TEST", 10);

        discount.Should().NotBeNull();
        discount.MaxDiscountAmount.Should().BeNull();
        discount.MinOrderAmount.Should().BeNull();
        discount.UsageLimit.Should().BeNull();
        discount.ExpiresAt.Should().BeNull();
    }
}