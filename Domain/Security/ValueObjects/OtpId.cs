namespace Domain.Security.ValueObjects;

public sealed record OtpId : StronglyTypedId<OtpId>
{
    private OtpId(Guid value) : base(value) { }

    public static implicit operator Guid(OtpId id) => id.Value;

    public override string ToString() => Value.ToString();
}
