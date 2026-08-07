using Domain.Order.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Order.ValueObjects;

public class OrderStatusIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        OrderStatusId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => OrderStatusId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        OrderStatusId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        OrderStatusId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }
}
