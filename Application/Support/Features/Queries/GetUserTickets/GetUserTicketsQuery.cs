namespace Application.Support.Features.Queries.GetUserTickets;

public sealed record GetUserTicketsQuery(
    int UserId,
    string? Status,
    int Page = 1,
    int PageSize = 20) : IRequest<ServiceResult<PaginatedResult<TicketDto>>>;