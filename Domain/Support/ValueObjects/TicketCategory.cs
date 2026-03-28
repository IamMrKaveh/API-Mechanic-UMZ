namespace Domain.Support.ValueObjects;

public sealed class TicketCategory : ValueObject
{
    public string Value { get; }

    private TicketCategory(string value) => Value = value;

    public static TicketCategory Create(string value)
    {
        Guard.Against.NullOrWhiteSpace(value, nameof(value));
        return new TicketCategory(value.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;
}