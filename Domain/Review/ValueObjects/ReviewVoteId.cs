namespace Domain.Review.ValueObjects;

public sealed record ReviewVoteId : StronglyTypedId<ReviewVoteId>
{
    private ReviewVoteId(Guid value) : base(value) { }

    public static implicit operator Guid(ReviewVoteId id) => id.Value;

    public override string ToString() => Value.ToString();
}
