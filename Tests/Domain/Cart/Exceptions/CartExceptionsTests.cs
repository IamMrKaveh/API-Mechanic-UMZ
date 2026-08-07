using Domain.Cart.Exceptions;
using Domain.Cart.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Cart.Exceptions;

public class CartExceptionsTests
{
    [Fact]
    public void CartItemNotFoundException_ExposesVariantIdAndErrorCodeAndMessage()
    {
        var variantId = VariantId.NewId();

        var sut = new CartItemNotFoundException(variantId);

        sut.VariantId.ShouldBe(variantId);
        sut.ErrorCode.ShouldBe("CART_ITEM_NOT_FOUND");
        sut.Message.ShouldContain(variantId.ToString());
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void CartAlreadyCheckedOutException_ExposesCartIdAndErrorCodeAndMessage()
    {
        var cartId = CartId.NewId();

        var sut = new CartAlreadyCheckedOutException(cartId);

        sut.CartId.ShouldBe(cartId);
        sut.ErrorCode.ShouldBe("CART_ALREADY_CHECKED_OUT");
        sut.Message.ShouldContain(cartId.ToString());
        sut.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public void InvalidCartQuantityException_ExposesQuantityAndErrorCodeAndMessage()
    {
        var sut = new InvalidCartQuantityException(-3);

        sut.Quantity.ShouldBe(-3);
        sut.ErrorCode.ShouldBe("INVALID_CART_QUANTITY");
        sut.Message.ShouldContain("-3");
        sut.ShouldBeAssignableTo<DomainException>();
    }
}
