namespace Domain.Media.ValueObjects;

public sealed record MediaId : StronglyTypedId<MediaId>
{
    private MediaId(Guid value) : base(value) { }

    public static implicit operator Guid(MediaId id) => id.Value;

    public override string ToString() => Value.ToString();
}
