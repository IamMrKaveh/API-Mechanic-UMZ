using Domain.Attribute.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Attribute.ValueObjects;

public class AttributeTypeIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        AttributeTypeId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_TwoInvocations_ProduceDifferentValues()
    {
        AttributeTypeId.NewId().Value.ShouldNotBe(AttributeTypeId.NewId().Value);
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        AttributeTypeId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => AttributeTypeId.From(Guid.Empty));
    }

    [Fact]
    public void ToString_ReturnsUnderlyingGuidToString()
    {
        var guid = Guid.NewGuid();

        AttributeTypeId.From(guid).ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();

        Guid unwrapped = AttributeTypeId.From(guid);

        unwrapped.ShouldBe(guid);
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        AttributeTypeId.From(guid).ShouldBe(AttributeTypeId.From(guid));
    }

    [Fact]
    public void Equality_ForRecordWithDifferentValue_TreatsInstancesAsUnequal()
    {
        AttributeTypeId.From(Guid.NewGuid()).ShouldNotBe(AttributeTypeId.From(Guid.NewGuid()));
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        AttributeTypeId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }
}
