using Domain.Audit.Events;
using Domain.Audit.ValueObjects;
using Domain.Common.Abstractions;
using Domain.User.ValueObjects;
using System;

namespace Domain.Audit.Entities;

public sealed class AuditLog : AggregateRoot<AuditLogId>
{
    public UserId? UserId { get; private set; }
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
        var auditLog = new AuditLog
        {
            Id = AuditLogId.NewId(),
            UserId = userId,
            EventType = eventType,
            Action = action,
            IpAddress = ipAddress,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            UserAgent = userAgent,
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
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}