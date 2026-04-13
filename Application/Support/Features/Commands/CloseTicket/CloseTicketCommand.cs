namespace Application.Support.Features.Commands.CloseTicket;

public record CloseTicketCommand(
    Guid TicketId,
    Guid UserId,
    bool IsAdmin) : IRequest<ServiceResult>;