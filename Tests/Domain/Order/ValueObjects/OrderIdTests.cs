using Domain.Order.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Order.ValueObjects;

public class OrderIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        OrderId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => OrderId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        OrderId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();

        Guid unwrapped = OrderId.From(guid);

        unwrapped.ShouldBe(guid);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        OrderId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        OrderId.From(guid).ShouldBe(OrderId.From(guid));
    }
}
