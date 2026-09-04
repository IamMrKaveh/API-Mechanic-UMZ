using Domain.Shipping.Exceptions;
using Domain.Shipping.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Shipping.Exceptions;

public class ShippingExceptionsTests
{
    [Fact]
    public void DefaultShippingCannotBeDeletedException_ExposesShippingIdAndErrorCode()
    {
        var id = ShippingId.NewId();

        var sut = new DefaultShippingCannotBeDeletedException(id);

        sut.ShippingId.ShouldBe(id);
        sut.ErrorCode.ShouldBe("DEFAULT_SHIPPING_CANNOT_BE_DELETED");
        sut.Message.ShouldContain(id.Value.ToString());
        sut.ShouldBeAssignableTo<DomainException>();
    }
}
