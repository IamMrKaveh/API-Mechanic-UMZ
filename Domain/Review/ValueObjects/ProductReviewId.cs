namespace Domain.Review.ValueObjects;

public sealed record ProductReviewId(Guid Value)
{
    public static ProductReviewId NewId() => new(Guid.NewGuid());
    public static ProductReviewId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}