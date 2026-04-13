namespace Application.Support.Features.Commands.ReplyToTicket;

public record ReplyToTicketCommand(
    Guid TicketId,
    Guid AdminId,
    string Message) : IRequest<ServiceResult>;