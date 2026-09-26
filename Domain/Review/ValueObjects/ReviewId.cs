namespace Domain.Review.ValueObjects;

public sealed record ReviewId : StronglyTypedId<ReviewId>
{
    private ReviewId(Guid value) : base(value) { }

    public static implicit operator Guid(ReviewId id) => id.Value;

    public override string ToString() => Value.ToString();
}
