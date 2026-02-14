namespace Application.Support.Features.Commands.ReplyToTicket;

public sealed record ReplyToTicketCommand(
    int TicketId,
    int SenderId,
    string Message,
    bool IsAdminReply) : IRequest<ServiceResult<bool>>;