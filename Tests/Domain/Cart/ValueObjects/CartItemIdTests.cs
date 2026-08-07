using Domain.Cart.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Cart.ValueObjects;

public class CartItemIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        CartItemId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => CartItemId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        CartItemId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        CartItemId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }
}
