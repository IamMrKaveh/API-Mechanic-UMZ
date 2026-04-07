namespace Domain.Audit.ValueObjects;

public sealed record AuditLogId
{
    public Guid Value { get; }

    private AuditLogId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("AuditLogId cannot be empty.", nameof(value));

        Value = value;
    }

    public static AuditLogId NewId() => new(Guid.NewGuid());

    public static AuditLogId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}