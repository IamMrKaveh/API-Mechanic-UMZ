using Domain.Discount.Results;

namespace Tests.Domain.Discount.Results;

public class DiscountValidationTests
{
    [Fact]
    public void Success_IsValidWithoutReason()
    {
        var sut = DiscountValidation.Success();

        sut.IsValid.ShouldBeTrue();
        sut.FailureReason.ShouldBeNull();
    }

    [Fact]
    public void Fail_IsInvalidWithReason()
    {
        var sut = DiscountValidation.Fail("Expired");

        sut.IsValid.ShouldBeFalse();
        sut.FailureReason.ShouldBe("Expired");
    }

    [Fact]
    public void Success_InstancesAreIndependent()
    {
        var first = DiscountValidation.Success();
        var second = DiscountValidation.Success();

        first.ShouldNotBeSameAs(second);
        first.IsValid.ShouldBe(second.IsValid);
    }
}
