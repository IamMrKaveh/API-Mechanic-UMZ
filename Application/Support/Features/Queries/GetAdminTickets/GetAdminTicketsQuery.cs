using Application.Common.Results;
using Application.Support.Features.Shared;
using SharedKernel.Models;

namespace Application.Support.Features.Queries.GetAdminTickets;

public sealed record GetAdminTicketsQuery(
    string? Status,
    string? Priority,
    Guid? UserId,
    int Page = 1,
    int PageSize = 20) : IRequest<ServiceResult<PaginatedResult<TicketDto>>>;