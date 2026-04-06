using Application.Common.Results;
using Application.Support.Features.Shared;

namespace Application.Support.Features.Queries.GetTicket;

public record GetTicketQuery(int TicketId, int RequestingUserId, bool IsAdmin) : IRequest<ServiceResult<TicketDto>>;