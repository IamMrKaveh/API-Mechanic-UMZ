using Domain.Audit.Events;
using Domain.Audit.ValueObjects;

namespace Tests.Domain.Audit.Events;

public class AuditLogCreatedEventTests
{
    [Fact]
    public void Constructor_ExposesArgumentsAsProperties()
    {
        var id = AuditLogId.NewId();

        var sut = new AuditLogCreatedEvent(id, "OrderCreated");

        sut.AuditLogId.ShouldBe(id);
        sut.Action.ShouldBe("OrderCreated");
    }

    [Fact]
    public void NewEvent_HasUniqueEventId()
    {
        var first = new AuditLogCreatedEvent(AuditLogId.NewId(), "A");
        var second = new AuditLogCreatedEvent(AuditLogId.NewId(), "B");

        first.EventId.ShouldNotBe(second.EventId);
    }

    [Fact]
    public void ChainedCorrelation_IsPreserved()
    {
        var correlationId = Guid.NewGuid();
        var sut = new AuditLogCreatedEvent(AuditLogId.NewId(), "A");

        sut.WithCorrelationId(correlationId);

        sut.CorrelationId.ShouldBe(correlationId);
    }
}
