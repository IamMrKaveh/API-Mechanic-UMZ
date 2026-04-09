using Application.Support.Features.Shared;

namespace Application.Support.Features.Queries.GetAdminTickets;

public sealed record GetAdminTicketsQuery(
    string? Status,
    string? Priority,
    Guid? UserId) : IRequest<ServiceResult<PaginatedResult<TicketDto>>>;