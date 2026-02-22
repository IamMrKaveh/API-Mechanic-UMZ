namespace Domain.Support.ValueObjects;

public sealed class TicketStatus : ValueObject
{
    public string Value { get; }
    public string DisplayName { get; }
    public bool IsClosed { get; }
    public bool RequiresResponse { get; }

    private TicketStatus(string value, string displayName, bool isClosed, bool requiresResponse)
    {
        Value = value;
        DisplayName = displayName;
        IsClosed = isClosed;
        RequiresResponse = requiresResponse;
    }

    public static TicketStatus Open => new("Open", "باز", false, true);
    public static TicketStatus AwaitingReply => new("AwaitingReply", "در انتظار پاسخ", false, true);
    public static TicketStatus Answered => new("Answered", "پاسخ داده شده", false, false);
    public static TicketStatus Closed => new("Closed", "بسته شده", true, false);

    public static TicketStatus FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Open;

        return value.ToLowerInvariant() switch
        {
            "open" => Open,
            "awaitingreply" => AwaitingReply,
            "answered" => Answered,
            "closed" => Closed,
            _ => Open
        };
    }

    public static IEnumerable<TicketStatus> GetAll()
    {
        yield return Open;
        yield return AwaitingReply;
        yield return Answered;
        yield return Closed;
    }

    public static IEnumerable<TicketStatus> GetOpenStatuses()
    {
        return GetAll().Where(s => !s.IsClosed);
    }

    public bool CanAddMessage() => !IsClosed;

    public bool CanClose() => !IsClosed;

    public bool CanReopen() => IsClosed;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => DisplayName;

    public static implicit operator string(TicketStatus status) => status.Value;
}