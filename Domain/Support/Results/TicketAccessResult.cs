namespace Domain.Support.Results;

public sealed class TicketAccessResult
{
    public bool HasAccess { get; private set; }
    public string? Error { get; private set; }

    private TicketAccessResult()
    { }

    public static TicketAccessResult Allowed() => new() { HasAccess = true };

    public static TicketAccessResult Denied(string error) => new() { HasAccess = false, Error = error };
}