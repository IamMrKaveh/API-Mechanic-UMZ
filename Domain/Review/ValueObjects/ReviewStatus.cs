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

    private static readonly IReadOnlyDictionary<string, ReviewStatus> All =
        new Dictionary<string, ReviewStatus>(StringComparer.OrdinalIgnoreCase)
        {
            [Pending.Value] = Pending,
            [Approved.Value] = Approved,
            [Rejected.Value] = Rejected
        };

    public static ReviewStatus FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Pending;

        if (All.TryGetValue(value, out var status))
            return status;

        throw new DomainException($"وضعیت نظر '{value}' نامعتبر است.");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => DisplayName;

    public static implicit operator string(ReviewStatus status) => status.Value;
}