using Application.Support.Features.Shared;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using Quartz.Util;

namespace Application.Support.Features.Queries.GetAdminTickets;

public sealed class GetAdminTicketsHandler(ITicketQueryService supportQueryService)
        : IRequestHandler<GetAdminTicketsQuery, ServiceResult<PaginatedResult<TicketDto>>>
{
    public async Task<ServiceResult<PaginatedResult<TicketDto>>> Handle(
        GetAdminTicketsQuery request,
        CancellationToken ct)
    {
        var ticketPriority = request.Priority.IsNullOrWhiteSpace()
            ? TicketPriority.Normal
            : TicketPriority.FromString(request.Priority);

        var ticketStatus = request.Status.IsNullOrWhiteSpace()
            ? TicketStatus.Open
            : TicketStatus.FromString(request.Status);

        var userId = UserId.From(request.UserId.Value);

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