using Application.Support.Features.Shared;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using Quartz.Util;

namespace Application.Support.Features.Queries.GetTickets;

public class GetTicketsHandler(
    ITicketQueryService supportQueryService) : IRequestHandler<GetTicketsQuery, ServiceResult<PaginatedResult<TicketListItemDto>>>
{
    public async Task<ServiceResult<PaginatedResult<TicketListItemDto>>> Handle(
        GetTicketsQuery request,
        CancellationToken ct)
    {
        var ticketPriority = request.Priority.IsNullOrWhiteSpace()
            ? TicketPriority.Normal
            : TicketPriority.FromString(request.Priority);

        var ticketStatus = request.Status.IsNullOrWhiteSpace()
            ? TicketStatus.Open
            : TicketStatus.FromString(request.Status);

        var userId = UserId.From(request.UserId.Value);

        var result = await supportQueryService.GetTicketsPagedAsync(
            userId,
            ticketStatus,
            ticketPriority,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<TicketListItemDto>>.Success(result);
    }
}