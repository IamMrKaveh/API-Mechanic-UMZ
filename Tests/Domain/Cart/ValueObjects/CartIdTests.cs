using Domain.Cart.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Cart.ValueObjects;

public class CartIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        CartId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => CartId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        CartId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();

        Guid unwrapped = CartId.From(guid);

        unwrapped.ShouldBe(guid);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        CartId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        CartId.From(guid).ShouldBe(CartId.From(guid));
    }
}
