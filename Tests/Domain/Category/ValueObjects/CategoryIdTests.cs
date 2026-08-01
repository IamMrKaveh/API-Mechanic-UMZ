using Domain.Category.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Category.ValueObjects;

public class CategoryIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        CategoryId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_TwoInvocations_ProduceDifferentValues()
    {
        CategoryId.NewId().Value.ShouldNotBe(CategoryId.NewId().Value);
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsCategoryIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        CategoryId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => CategoryId.From(Guid.Empty));
    }

    [Fact]
    public void ToString_ReturnsUnderlyingGuidToString()
    {
        var guid = Guid.NewGuid();

        CategoryId.From(guid).ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();

        Guid unwrapped = CategoryId.From(guid);

        unwrapped.ShouldBe(guid);
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        CategoryId.From(guid).ShouldBe(CategoryId.From(guid));
    }

    [Fact]
    public void Equality_ForRecordWithDifferentValue_TreatsInstancesAsUnequal()
    {
        CategoryId.From(Guid.NewGuid()).ShouldNotBe(CategoryId.From(Guid.NewGuid()));
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        CategoryId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }
}
