namespace Application.Support.Features.Queries.GetAdminTickets;

public sealed record GetAdminTicketsQuery(
    string? Status,
    string? Priority,
    int? UserId,
    int Page = 1,
    int PageSize = 20) : IRequest<ServiceResult<PaginatedResult<TicketDto>>>;