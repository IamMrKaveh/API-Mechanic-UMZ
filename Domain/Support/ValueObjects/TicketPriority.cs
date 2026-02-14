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

    public static TicketPriority Low => new("Low", "کم", 1);
    public static TicketPriority Normal => new("Normal", "معمولی", 2);
    public static TicketPriority High => new("High", "زیاد", 3);
    public static TicketPriority Urgent => new("Urgent", "فوری", 4);

    public static TicketPriority FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Normal;

        return value.ToLowerInvariant() switch
        {
            "low" => Low,
            "normal" => Normal,
            "high" => High,
            "urgent" => Urgent,
            _ => Normal
        };
    }

    public static IEnumerable<TicketPriority> GetAll()
    {
        yield return Low;
        yield return Normal;
        yield return High;
        yield return Urgent;
    }

    public bool IsHighPriority() => SortOrder >= High.SortOrder;

    public bool IsUrgent() => this == Urgent;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => DisplayName;

    public static implicit operator string(TicketPriority priority) => priority.Value;

    public static bool operator >(TicketPriority left, TicketPriority right) =>
        left.SortOrder > right.SortOrder;

    public static bool operator <(TicketPriority left, TicketPriority right) =>
        left.SortOrder < right.SortOrder;

    public static bool operator >=(TicketPriority left, TicketPriority right) =>
        left.SortOrder >= right.SortOrder;

    public static bool operator <=(TicketPriority left, TicketPriority right) =>
        left.SortOrder <= right.SortOrder;
}