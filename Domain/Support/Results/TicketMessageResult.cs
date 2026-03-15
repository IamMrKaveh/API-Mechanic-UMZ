using Domain.Support.Aggregates;

namespace Domain.Support.Results;

public sealed class TicketMessageResult
{
    public bool IsSuccess { get; private set; }
    public TicketMessage? Message { get; private set; }
    public string? Error { get; private set; }

    private TicketMessageResult()
    { }

    public static TicketMessageResult Success(TicketMessage message) =>
        new() { IsSuccess = true, Message = message };

    public static TicketMessageResult Failed(string error) =>
        new() { IsSuccess = false, Error = error };
}