namespace Application.Support.Features.Commands.ReplyToTicket;

public sealed record ReplyToTicketCommand(int TicketId, string Message) : IRequest<ServiceResult>;