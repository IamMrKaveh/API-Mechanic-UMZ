namespace Application.Support.Features.Commands.CloseTicket;

public sealed record CloseTicketCommand(
    int TicketId,
    int UserId,
    bool IsAdmin) : IRequest<ServiceResult<bool>>;