namespace Application.Support.Features.Commands.CloseTicket;

public record CloseTicketCommand(Guid TicketId) : IRequest<ServiceResult>;