using Application.Support.Features.Shared;

namespace Application.Support.Features.Queries.GetTickets;

public record GetTicketsQuery(
    string? Status,
    string? Priority,
    int Page = 1,
    int PageSize = 10)
    : IPageQuery<TicketListItemDto>;