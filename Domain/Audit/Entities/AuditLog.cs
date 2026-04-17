using Domain.Audit.Events;
using Domain.Audit.ValueObjects;
using Domain.User.ValueObjects;
using System.Text;

namespace Domain.Audit.Entities;

public sealed class AuditLog : AggregateRoot<AuditLogId>
{
    public UserId? UserId { get; private set; }
    public User.Aggregates.User? User { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string? Details { get; private set; }
    public string IpAddress { get; private set; } = null!;
    public string? UserAgent { get; private set; }
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string IntegrityHash { get; private set; } = null!;
    public bool IsArchived { get; private set; }
    public DateTime? ArchivedAt { get; private set; }

    private AuditLog()
    { }

    public static AuditLog Create(
        UserId? userId,
        string eventType,
        string action,
        string ipAddress,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        string? userAgent = null)
    {
        Guard.Against.NullOrWhiteSpace(eventType, nameof(eventType));
        Guard.Against.NullOrWhiteSpace(action, nameof(action));
        Guard.Against.NullOrWhiteSpace(ipAddress, nameof(ipAddress));

        var auditLog = new AuditLog
        {
            Id = AuditLogId.NewId(),
            UserId = userId,
            EventType = eventType.Trim(),
            Action = action.Trim(),
            IpAddress = ipAddress.Trim(),
            EntityType = entityType?.Trim(),
            EntityId = entityId?.Trim(),
            Details = details?.Trim(),
            UserAgent = userAgent?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        auditLog.IntegrityHash = auditLog.ComputeHash();
        auditLog.RaiseDomainEvent(new AuditLogCreatedEvent(auditLog.Id, auditLog.Action));

        return auditLog;
    }

    public void MarkAsArchived()
    {
        if (IsArchived) return;
        IsArchived = true;
        ArchivedAt = DateTime.UtcNow;
    }

    public bool VerifyIntegrity()
    {
        return string.Equals(IntegrityHash, ComputeHash(), StringComparison.Ordinal);
    }

    private string ComputeHash()
    {
        var userIdString = UserId?.Value.ToString() ?? "null";
        var data = $"{userIdString}|{EventType}|{Action}|{Details}|{IpAddress}|{CreatedAt:O}";
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}