using Domain.Security.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Security.ValueObjects;

public class SessionIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        SessionId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_TwoInvocations_ProduceDifferentValues()
    {
        SessionId.NewId().Value.ShouldNotBe(SessionId.NewId().Value);
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        SessionId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => SessionId.From(Guid.Empty));
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();

        Guid unwrapped = SessionId.From(guid);

        unwrapped.ShouldBe(guid);
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        SessionId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }

    [Fact]
    public void ToString_ReturnsGuidStringRepresentation()
    {
        var guid = Guid.NewGuid();

        SessionId.From(guid).ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        SessionId.From(guid).ShouldBe(SessionId.From(guid));
    }

    [Fact]
    public void Equality_ForRecordWithDifferentValue_TreatsInstancesAsUnequal()
    {
        SessionId.From(Guid.NewGuid()).ShouldNotBe(SessionId.From(Guid.NewGuid()));
    }
}
