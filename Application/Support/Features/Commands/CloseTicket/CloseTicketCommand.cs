using Application.Common.Results;

namespace Application.Support.Features.Commands.CloseTicket;

public record CloseTicketCommand(Guid TicketId, Guid UserId, bool IsAdmin = false) : IRequest<ServiceResult>;