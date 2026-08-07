using Domain.Audit.Events;
using Domain.Audit.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Audit.Entities;

public sealed class AuditLog : AggregateRoot<AuditLogId>
{
    public const int CurrentHashVersion = 2;

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
    public int HashVersion { get; private set; }
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
            CreatedAt = TruncateToMicroseconds(DateTime.UtcNow),
            HashVersion = CurrentHashVersion
        };

        auditLog.IntegrityHash = auditLog.ComputeHash(CurrentHashVersion);
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
        var effectiveVersion = HashVersion <= 0 ? 1 : HashVersion;
        var recomputed = ComputeHash(effectiveVersion);
        return string.Equals(IntegrityHash, recomputed, StringComparison.Ordinal);
    }

    public string RecomputeIntegrityHash()
    {
        var effectiveVersion = HashVersion <= 0 ? 1 : HashVersion;
        return ComputeHash(effectiveVersion);
    }

    public void UpgradeHashVersion()
    {
        if (HashVersion >= CurrentHashVersion) return;
        HashVersion = CurrentHashVersion;
        IntegrityHash = ComputeHash(CurrentHashVersion);
        IncrementVersion();
    }

    private string ComputeHash(int version)
    {
        var userIdString = UserId?.Value.ToString() ?? "null";

        var data = version switch
        {
            1 => string.Create(CultureInfo.InvariantCulture,
                $"{userIdString}|{EventType}|{Action}|{Details}|{IpAddress}|{CreatedAt:O}"),
            _ => string.Join('|',
                userIdString,
                EventType,
                Action,
                EntityType ?? "null",
                EntityId ?? "null",
                Details ?? "null",
                IpAddress,
                UserAgent ?? "null",
                CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ", CultureInfo.InvariantCulture))
        };

        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    private static DateTime TruncateToMicroseconds(DateTime value)
    {
        var kind = value.Kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : value.Kind;
        var utc = kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        var truncatedTicks = utc.Ticks - (utc.Ticks % 10L);
        return new DateTime(truncatedTicks, DateTimeKind.Utc);
    }
}
