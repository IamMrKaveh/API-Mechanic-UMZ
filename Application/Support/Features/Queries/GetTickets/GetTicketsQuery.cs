using Application.Common.Results;
using Application.Support.Features.Shared;
using SharedKernel.Models;

namespace Application.Support.Features.Queries.GetTickets;

public record GetTicketsQuery(
    Guid? UserId,
    string? Status,
    string? Priority,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<TicketListItemDto>>>;