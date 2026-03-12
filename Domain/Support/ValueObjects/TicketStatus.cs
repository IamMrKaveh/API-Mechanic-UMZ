namespace Domain.Support.ValueObjects;

public sealed class TicketStatus : ValueObject
{
    public string Value { get; }
    public string DisplayName { get; }
    public bool IsClosed { get; }
    public bool RequiresResponse { get; }
    public int SortOrder { get; }

    private TicketStatus(string value, string displayName, bool isClosed, bool requiresResponse, int sortOrder)
    {
        Value = value;
        DisplayName = displayName;
        IsClosed = isClosed;
        RequiresResponse = requiresResponse;
        SortOrder = sortOrder;
    }

    public static readonly TicketStatus Open = new("Open", "باز", false, true, 1);
    public static readonly TicketStatus AwaitingReply = new("AwaitingReply", "در انتظار پاسخ", false, true, 2);
    public static readonly TicketStatus Answered = new("Answered", "پاسخ داده شده", false, false, 3);
    public static readonly TicketStatus Closed = new("Closed", "بسته شده", true, false, 4);

    private static readonly IReadOnlyDictionary<string, TicketStatus> All =
        new Dictionary<string, TicketStatus>(StringComparer.OrdinalIgnoreCase)
        {
            [Open.Value] = Open,
            [AwaitingReply.Value] = AwaitingReply,
            [Answered.Value] = Answered,
            [Closed.Value] = Closed
        };

    public static TicketStatus FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Open;

        if (All.TryGetValue(value, out var status))
            return status;

        return Open;
    }

    public static TicketStatus Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("وضعیت تیکت نمی‌تواند خالی باشد.");

        if (All.TryGetValue(value, out var status))
            return status;

        throw new DomainException($"وضعیت تیکت '{value}' نامعتبر است.");
    }

    public static IEnumerable<TicketStatus> GetAll() => All.Values;

    public static IEnumerable<TicketStatus> GetOpenStatuses() =>
        All.Values.Where(s => !s.IsClosed);

    public bool CanAddMessage() => !IsClosed;

    public bool CanClose() => !IsClosed;

    public bool CanReopen() => IsClosed;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => DisplayName;

    public static implicit operator string(TicketStatus status) => status.Value;

    public static bool operator >(TicketStatus left, TicketStatus right) => left.SortOrder > right.SortOrder;

    public static bool operator <(TicketStatus left, TicketStatus right) => left.SortOrder < right.SortOrder;

    public static bool operator >=(TicketStatus left, TicketStatus right) => left.SortOrder >= right.SortOrder;

    public static bool operator <=(TicketStatus left, TicketStatus right) => left.SortOrder <= right.SortOrder;
}