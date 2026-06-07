namespace Domain.Review.ValueObjects;

public sealed class ReviewStatus : ValueObject
{
    public string Value { get; }
    public string DisplayName { get; }

    private ReviewStatus(string value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public static readonly ReviewStatus Pending = new("Pending", "در انتظار تأیید");
    public static readonly ReviewStatus Approved = new("Approved", "تأیید شده");
    public static readonly ReviewStatus Rejected = new("Rejected", "رد شده");

    public ReviewStatus()
    {
    }

    public static ReviewStatus From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("ReviewStatus value cannot be empty.");

        return value.Trim() switch
        {
            "Pending" => Pending,
            "Approved" => Approved,
            "Rejected" => Rejected,
            _ => throw new DomainException($"Unknown ReviewStatus value '{value}'.")
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => DisplayName;

    public static implicit operator string(ReviewStatus status) => status.Value;
}