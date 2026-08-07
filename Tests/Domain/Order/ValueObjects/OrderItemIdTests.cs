using Domain.Order.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Order.ValueObjects;

public class OrderItemIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        OrderItemId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => OrderItemId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        OrderItemId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        OrderItemId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }
}
