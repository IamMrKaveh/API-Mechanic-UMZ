using Application.Support.Features.Shared;

namespace Application.Support.Features.Queries.GetTickets;

public record GetTicketsQuery(
    Guid? UserId,
    string? Status,
    string? Priority) : IRequest<ServiceResult<PaginatedResult<TicketListItemDto>>>;