namespace Domain.Audit.ValueObjects;

public sealed record AuditLogId : StronglyTypedId<AuditLogId>
{
    private AuditLogId(Guid value) : base(value) { }

    public static implicit operator Guid(AuditLogId id) => id.Value;

    public override string ToString() => Value.ToString();
}
