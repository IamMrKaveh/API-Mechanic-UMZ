namespace Domain.Review.ValueObjects;

public sealed record ReviewId : IStronglyTypedId
{
    public Guid Value { get; }

    private ReviewId(Guid value) => Value = value;

    public static ReviewId NewId() => new(Guid.NewGuid());

    public static ReviewId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("ReviewId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(ReviewId id) => id.Value;
}