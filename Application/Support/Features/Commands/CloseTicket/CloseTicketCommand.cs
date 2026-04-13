namespace Application.Support.Features.Commands.CloseTicket;

public record CloseTicketCommand(
    Guid TicketId,
    Guid AdminId,
    string Resolution) : IRequest<ServiceResult>;