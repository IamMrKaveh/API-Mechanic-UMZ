using Application.Support.Features.Shared;

namespace Application.Support.Features.Queries.GetTicket;

public record GetTicketQuery(Guid TicketId, Guid RequestingUserId, bool IsAdmin) : IRequest<ServiceResult<TicketDto>>;