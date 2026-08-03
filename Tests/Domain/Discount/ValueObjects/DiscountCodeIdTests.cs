using Domain.Discount.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Discount.ValueObjects;

public class DiscountCodeIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        DiscountCodeId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => DiscountCodeId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        DiscountCodeId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        DiscountCodeId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        DiscountCodeId.From(guid).ShouldBe(DiscountCodeId.From(guid));
    }
}
