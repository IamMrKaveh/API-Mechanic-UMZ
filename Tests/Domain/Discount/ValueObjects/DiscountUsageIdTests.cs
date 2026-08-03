using Domain.Discount.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Discount.ValueObjects;

public class DiscountUsageIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        DiscountUsageId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => DiscountUsageId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        DiscountUsageId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        DiscountUsageId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }
}
