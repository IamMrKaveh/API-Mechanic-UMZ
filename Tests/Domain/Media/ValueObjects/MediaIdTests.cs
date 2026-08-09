using Domain.Media.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Media.ValueObjects;

public class MediaIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        MediaId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_TwoConsecutiveInvocations_ProduceDifferentValues()
    {
        MediaId.NewId().Value.ShouldNotBe(MediaId.NewId().Value);
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        MediaId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => MediaId.From(Guid.Empty));
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();

        Guid unwrapped = MediaId.From(guid);

        unwrapped.ShouldBe(guid);
    }

    [Fact]
    public void ToString_ReturnsUnderlyingGuidString()
    {
        var guid = Guid.NewGuid();

        MediaId.From(guid).ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        MediaId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        MediaId.From(guid).ShouldBe(MediaId.From(guid));
    }

    [Fact]
    public void Equality_ForRecordWithDifferentValues_TreatsInstancesAsUnequal()
    {
        MediaId.NewId().ShouldNotBe(MediaId.NewId());
    }
}

