using Domain.Audit.ValueObjects;

namespace Domain.Audit.Events;

public sealed record AuditLogCreatedEvent(AuditLogId AuditLogId, string Action) : DomainEvent;
