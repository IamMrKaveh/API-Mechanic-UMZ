namespace Application.Support.Features.Commands.ReplyToTicket;

public record ReplyToTicketCommand(
    Guid TicketId,
    Guid SenderId,
    string Content,
    bool IsAdmin = false) : IRequest<ServiceResult>;