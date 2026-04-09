using Application.Support.Features.Shared;

namespace Application.Support.Features.Queries.GetTickets;

public record GetTicketsQuery(
    Guid? UserId,
    string? Status,
    string? Priority,
    int Page = 1,
    int Pagesize = 10) : IRequest<ServiceResult<PaginatedResult<TicketListItemDto>>>;