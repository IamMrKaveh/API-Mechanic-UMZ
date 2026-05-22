namespace Domain.Review.ValueObjects;

public sealed class ReviewStatus : ValueObject
{
    public string Value { get; }
    public string DisplayName { get; }

    public ReviewStatus()
    {
    }

    private ReviewStatus(string value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public static readonly ReviewStatus Pending = new("Pending", "در انتظار تأیید");
    public static readonly ReviewStatus Approved = new("Approved", "تأیید شده");
    public static readonly ReviewStatus Rejected = new("Rejected", "رد شده");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => DisplayName;

    public static implicit operator string(ReviewStatus status) => status.Value;
}