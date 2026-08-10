using Domain.Support.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Support.ValueObjects;

public class TicketIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        TicketId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_TwoInvocations_ProduceDifferentValues()
    {
        TicketId.NewId().Value.ShouldNotBe(TicketId.NewId().Value);
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        TicketId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => TicketId.From(Guid.Empty));
    }

    [Fact]
    public void ToString_ReturnsUnderlyingGuidToString()
    {
        var guid = Guid.NewGuid();

        TicketId.From(guid).ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();

        Guid unwrapped = TicketId.From(guid);

        unwrapped.ShouldBe(guid);
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        TicketId.From(guid).ShouldBe(TicketId.From(guid));
    }

    [Fact]
    public void Equality_ForRecordWithDifferentValue_TreatsInstancesAsUnequal()
    {
        TicketId.From(Guid.NewGuid()).ShouldNotBe(TicketId.From(Guid.NewGuid()));
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        TicketId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }
}
