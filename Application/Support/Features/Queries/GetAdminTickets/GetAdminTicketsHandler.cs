using Application.Support.Features.Shared;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Support.Features.Queries.GetAdminTickets;

public sealed class GetAdminTicketsHandler(
    ITicketQueryService supportQueryService,
    ICurrentUserService currentUserService)
        : IRequestHandler<GetAdminTicketsQuery, ServiceResult<PaginatedResult<TicketDto>>>
{
    public async Task<ServiceResult<PaginatedResult<TicketDto>>> Handle(
        GetAdminTicketsQuery request,
        CancellationToken ct)
    {
        var ticketPriority = string.IsNullOrWhiteSpace(request.Priority)
            ? TicketPriority.Normal
            : TicketPriority.FromString(request.Priority);

        var ticketStatus = string.IsNullOrWhiteSpace(request.Status)
            ? TicketStatus.Open
            : TicketStatus.FromString(request.Status);

        UserId? userId = null;

        var result = await supportQueryService.GetAdminTicketsPagedAsync(
            ticketStatus,
            ticketPriority,
            userId,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<TicketDto>>.Success(result);
    }
}