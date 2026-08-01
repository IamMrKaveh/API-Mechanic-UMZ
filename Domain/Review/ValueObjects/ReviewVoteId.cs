namespace Domain.Review.ValueObjects;

public sealed record ReviewVoteId : IStronglyTypedId
{
    public Guid Value { get; }

    private ReviewVoteId(Guid value) => Value = value;

    public static ReviewVoteId NewId() => new(Guid.NewGuid());

    public static ReviewVoteId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("ReviewVoteId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(ReviewVoteId id) => id.Value;
}
