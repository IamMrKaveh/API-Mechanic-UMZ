namespace Domain.Audit.ValueObjects;

public sealed record AuditLogId : IStronglyTypedId
{
    public Guid Value { get; }

    private AuditLogId(Guid value) => Value = value;

    public static AuditLogId NewId() => new(Guid.NewGuid());

    public static AuditLogId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("AuditLogId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(AuditLogId id) => id.Value;
}