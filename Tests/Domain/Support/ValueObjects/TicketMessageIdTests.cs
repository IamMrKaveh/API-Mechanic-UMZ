using Domain.Support.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Support.ValueObjects;

public class TicketMessageIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        TicketMessageId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_TwoInvocations_ProduceDifferentValues()
    {
        TicketMessageId.NewId().Value.ShouldNotBe(TicketMessageId.NewId().Value);
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        TicketMessageId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => TicketMessageId.From(Guid.Empty));
    }

    [Fact]
    public void ToString_ReturnsUnderlyingGuidToString()
    {
        var guid = Guid.NewGuid();

        TicketMessageId.From(guid).ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();

        Guid unwrapped = TicketMessageId.From(guid);

        unwrapped.ShouldBe(guid);
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        TicketMessageId.From(guid).ShouldBe(TicketMessageId.From(guid));
    }

    [Fact]
    public void Equality_ForRecordWithDifferentValue_TreatsInstancesAsUnequal()
    {
        TicketMessageId.From(Guid.NewGuid()).ShouldNotBe(TicketMessageId.From(Guid.NewGuid()));
    }

    [Fact]
    public void ImplementsIStronglyTypedId()
    {
        TicketMessageId.NewId().ShouldBeAssignableTo<IStronglyTypedId>();
    }
}
