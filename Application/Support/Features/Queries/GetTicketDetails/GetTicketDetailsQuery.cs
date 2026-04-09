using Application.Common.Results;

namespace Application.Support.Features.Queries.GetTicketDetails;

public sealed record GetTicketDetailsQuery(
    Guid TicketId,
    Guid UserId,
    bool IsAdmin) : IRequest<ServiceResult<TicketDetailDto>>;