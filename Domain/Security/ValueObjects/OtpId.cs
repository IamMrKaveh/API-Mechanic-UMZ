namespace Domain.Security.ValueObjects;

public sealed record OtpId : IStronglyTypedId
{
    public Guid Value { get; }

    private OtpId(Guid value) => Value = value;

    public static OtpId NewId() => new(Guid.NewGuid());

    public static OtpId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("OtpId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(OtpId id) => id.Value;
}