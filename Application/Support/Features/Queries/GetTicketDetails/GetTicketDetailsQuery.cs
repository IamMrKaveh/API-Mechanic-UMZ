namespace Application.Support.Features.Queries.GetTicketDetails;

public sealed record GetTicketDetailsQuery(
    int TicketId,
    int UserId,
    bool IsAdmin) : IRequest<ServiceResult<TicketDetailDto>>;