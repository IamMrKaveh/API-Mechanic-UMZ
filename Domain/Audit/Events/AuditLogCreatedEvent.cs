using Domain.Audit.ValueObjects;
using Domain.Common.Events;

namespace Domain.Audit.Events;

public sealed class AuditLogCreatedEvent(AuditLogId auditLogId, string action) : DomainEvent
{
    public AuditLogId AuditLogId { get; } = auditLogId;
    public string Action { get; } = action;
}