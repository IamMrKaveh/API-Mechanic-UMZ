using Domain.Attribute.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Attribute.ValueObjects;

public class AttributeValueIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        AttributeValueId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_TwoInvocations_ProduceDifferentValues()
    {
        AttributeValueId.NewId().Value.ShouldNotBe(AttributeValueId.NewId().Value);
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        AttributeValueId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => AttributeValueId.From(Guid.Empty));
    }

    [Fact]
    public void ToString_ReturnsUnderlyingGuidToString()
    {
        var guid = Guid.NewGuid();

        AttributeValueId.From(guid).ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();

        Guid unwrapped = AttributeValueId.From(guid);

        unwrapped.ShouldBe(guid);
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        AttributeValueId.From(guid).ShouldBe(AttributeValueId.From(guid));
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        AttributeValueId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }
}
