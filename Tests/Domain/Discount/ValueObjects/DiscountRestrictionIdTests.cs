using Domain.Discount.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Discount.ValueObjects;

public class DiscountRestrictionIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        DiscountRestrictionId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => DiscountRestrictionId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        DiscountRestrictionId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        DiscountRestrictionId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }
}
