using Domain.Brand.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Brand.ValueObjects;

public class BrandIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        BrandId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_TwoInvocations_ProduceDifferentValues()
    {
        BrandId.NewId().Value.ShouldNotBe(BrandId.NewId().Value);
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsBrandIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        BrandId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => BrandId.From(Guid.Empty));
    }

    [Fact]
    public void ToString_ReturnsUnderlyingGuidToString()
    {
        var guid = Guid.NewGuid();
        var sut = BrandId.From(guid);

        sut.ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();

        Guid unwrapped = BrandId.From(guid);

        unwrapped.ShouldBe(guid);
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        BrandId.From(guid).ShouldBe(BrandId.From(guid));
    }

    [Fact]
    public void Equality_ForRecordWithDifferentValue_TreatsInstancesAsUnequal()
    {
        BrandId.From(Guid.NewGuid()).ShouldNotBe(BrandId.From(Guid.NewGuid()));
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        var sut = BrandId.NewId();

        sut.ShouldBeAssignableTo<IStronglyTypedId>();
    }
}
