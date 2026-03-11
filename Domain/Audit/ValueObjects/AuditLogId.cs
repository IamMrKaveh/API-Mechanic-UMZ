namespace Domain.Audit.ValueObjects;

public sealed record AuditLogId(Guid Value)
{
    public static AuditLogId NewId() => new(Guid.NewGuid());
    public static AuditLogId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}