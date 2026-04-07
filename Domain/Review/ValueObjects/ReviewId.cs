namespace Domain.Review.ValueObjects;

public sealed record ReviewId
{
    public Guid Value { get; }

    private ReviewId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("ProductReviewId cannot be empty.", nameof(value));

        Value = value;
    }

    public static ReviewId NewId() => new(Guid.NewGuid());

    public static ReviewId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}