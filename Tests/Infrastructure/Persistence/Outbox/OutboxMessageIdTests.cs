using System.Data;
using Infrastructure.Persistence.Outbox;

namespace Tests.Infrastructure.Persistence.Outbox;

public class OutboxMessageIdTests
{
    [Fact]
    public void NewId_ProducesNonEmptyGuidValue()
    {
        OutboxMessageId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsNoNullAllowedException()
    {
        Should.Throw<NoNullAllowedException>(() => OutboxMessageId.From(Guid.Empty));
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithThatValue()
    {
        var guid = Guid.NewGuid();

        OutboxMessageId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();

        Guid unwrapped = OutboxMessageId.From(guid);

        unwrapped.ShouldBe(guid);
    }

    [Fact]
    public void ToString_ReturnsUnderlyingGuidStringRepresentation()
    {
        var guid = Guid.NewGuid();

        OutboxMessageId.From(guid).ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void Equality_ForRecordStructWithSameValue_TreatsInstancesAsEqual()
    {
        var guid = Guid.NewGuid();

        OutboxMessageId.From(guid).ShouldBe(OutboxMessageId.From(guid));
    }

    [Fact]
    public void Equality_ForRecordStructWithDifferentValues_TreatsInstancesAsNotEqual()
    {
        OutboxMessageId.NewId().ShouldNotBe(OutboxMessageId.NewId());
    }

    [Fact]
    public void NewId_TwoConsecutiveCalls_ProduceDistinctValues()
    {
        OutboxMessageId.NewId().Value.ShouldNotBe(OutboxMessageId.NewId().Value);
    }
}
