using Domain.Discount.Exceptions;
using Domain.Discount.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Discount.Exceptions;

public class DiscountExceptionsTests
{
    [Fact]
    public void DiscountCodeNotRedeemableException_ExposesIdAndCodeAndErrorCodeAndMessage()
    {
        var id = DiscountCodeId.NewId();

        var sut = new DiscountCodeNotRedeemableException(id, "SAVE10");

        sut.Id.ShouldBe(id);
        sut.Code.ShouldBe("SAVE10");
        sut.ErrorCode.ShouldBe("DISCOUNT_CODE_NOT_REDEEMABLE");
        sut.Message.ShouldContain("SAVE10");
        sut.Message.ShouldContain(id.ToString());
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void InvalidDiscountException_WithMessageOnly_ExposesMessageAndNullDiscountCodeAndErrorCode()
    {
        var sut = new InvalidDiscountException("something is off");

        sut.Message.ShouldBe("something is off");
        sut.DiscountCode.ShouldBeNull();
        sut.ErrorCode.ShouldBe("INVALID_DISCOUNT");
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void InvalidDiscountException_WithMessageAndCode_ExposesBoth()
    {
        var sut = new InvalidDiscountException("bad", "SAVE10");

        sut.Message.ShouldBe("bad");
        sut.DiscountCode.ShouldBe("SAVE10");
    }
}
