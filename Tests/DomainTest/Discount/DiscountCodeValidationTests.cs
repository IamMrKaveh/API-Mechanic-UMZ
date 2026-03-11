namespace Tests.DomainTest.Discount;

public class DiscountCodeValidationTests
{
    [Fact]
    public void Validate_ActiveDiscountWithValidOrder_ShouldReturnValid()
    {
        var discount = new DiscountCodeBuilder().Build();

        var (isValid, error) = discount.Validate(1000, DateTime.UtcNow, 1, 0);

        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Validate_InactiveDiscount_ShouldReturnInvalid()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Deactivate();

        var (isValid, error) = discount.Validate(1000, DateTime.UtcNow, 1, 0);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_DeletedDiscount_ShouldReturnInvalid()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Delete();

        var (isValid, error) = discount.Validate(1000, DateTime.UtcNow, 1, 0);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_ExpiredDiscount_ShouldReturnInvalid()
    {
        var discount = new DiscountCodeBuilder()
            .AlreadyExpired()
            .Build();

        var (isValid, error) = discount.Validate(1000, DateTime.UtcNow, 1, 0);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_NotYetStartedDiscount_ShouldReturnInvalid()
    {
        var discount = new DiscountCodeBuilder()
            .NotYetStarted()
            .Build();

        var (isValid, error) = discount.Validate(1000, DateTime.UtcNow, 1, 0);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_OrderBelowMinimum_ShouldReturnInvalid()
    {
        var discount = new DiscountCodeBuilder()
            .WithMinOrderAmount(5000)
            .Build();

        var (isValid, error) = discount.Validate(1000, DateTime.UtcNow, 1, 0);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_OrderExactlyAtMinimum_ShouldReturnValid()
    {
        var discount = new DiscountCodeBuilder()
            .WithMinOrderAmount(1000)
            .Build();

        var (isValid, error) = discount.Validate(1000, DateTime.UtcNow, 1, 0);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UserReachedUsageLimit_ShouldReturnInvalid()
    {
        var discount = new DiscountCodeBuilder()
            .WithMaxUsagePerUser(2)
            .Build();

        var (isValid, error) = discount.Validate(1000, DateTime.UtcNow, 1, 2);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_GlobalUsageLimitReached_ShouldReturnInvalid()
    {
        var discount = new DiscountCodeBuilder()
            .WithUsageLimit(1)
            .Build();

        discount.IncrementUsage();

        var (isValid, error) = discount.Validate(1000, DateTime.UtcNow, 2, 0);

        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateForApplication_ShouldReturnRichValidationResult()
    {
        var discount = new DiscountCodeBuilder().Build();

        var result = discount.ValidateForApplication(1000, DateTime.UtcNow, 1, 0);

        result.IsValid.Should().BeTrue();
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public void ValidateForApplication_WithInvalidDiscount_ShouldReturnErrorMessage()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Deactivate();

        var result = discount.ValidateForApplication(1000, DateTime.UtcNow, 1, 0);

        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().NotBeNullOrEmpty();
    }
}