namespace Application.Audit.Features.Shared;

public class AuditDtos
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AuditLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; }

    public string Details { get; set; }

    public string IpAddress { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string EventType { get; set; }

    public string? UserAgent { get; set; }
}