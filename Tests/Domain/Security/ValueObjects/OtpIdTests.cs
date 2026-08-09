using Domain.Security.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Security.ValueObjects;

public class OtpIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        OtpId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_TwoInvocations_ProduceDifferentValues()
    {
        OtpId.NewId().Value.ShouldNotBe(OtpId.NewId().Value);
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        OtpId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => OtpId.From(Guid.Empty));
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();

        Guid unwrapped = OtpId.From(guid);

        unwrapped.ShouldBe(guid);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        OtpId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }

    [Fact]
    public void ToString_ReturnsGuidStringRepresentation()
    {
        var guid = Guid.NewGuid();

        OtpId.From(guid).ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        OtpId.From(guid).ShouldBe(OtpId.From(guid));
    }

    [Fact]
    public void Equality_ForRecordWithDifferentValue_TreatsInstancesAsUnequal()
    {
        OtpId.From(Guid.NewGuid()).ShouldNotBe(OtpId.From(Guid.NewGuid()));
    }
}
