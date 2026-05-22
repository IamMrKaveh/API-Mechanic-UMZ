namespace Domain.Support.ValueObjects;

public sealed class TicketPriority : ValueObject
{
    public string Value { get; }
    public string DisplayName { get; }
    public int SortOrder { get; }

    private TicketPriority(string value, string displayName, int sortOrder)
    {
        Value = value;
        DisplayName = displayName;
        SortOrder = sortOrder;
    }

    public static readonly TicketPriority Low = new("Low", "کم", 1);
    public static readonly TicketPriority Normal = new("Normal", "معمولی", 2);
    public static readonly TicketPriority High = new("High", "زیاد", 3);
    public static readonly TicketPriority Urgent = new("Urgent", "فوری", 4);

    private static readonly IReadOnlyDictionary<string, TicketPriority> All =
        new Dictionary<string, TicketPriority>(StringComparer.OrdinalIgnoreCase)
        {
            [Low.Value] = Low,
            [Normal.Value] = Normal,
            [High.Value] = High,
            [Urgent.Value] = Urgent
        };

    public static TicketPriority FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Normal;

        if (All.TryGetValue(value, out var priority))
            return priority;

        return Normal;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }
}