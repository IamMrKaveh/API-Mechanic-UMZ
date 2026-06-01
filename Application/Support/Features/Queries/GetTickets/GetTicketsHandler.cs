using Application.Support.Features.Shared;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Support.Features.Queries.GetTickets;

public class GetTicketsHandler(
    ITicketQueryService supportQueryService) : IRequestHandler<GetTicketsQuery, ServiceResult<PaginatedResult<TicketListItemDto>>>
{
    public async Task<ServiceResult<PaginatedResult<TicketListItemDto>>> Handle(
        GetTicketsQuery request,
        CancellationToken ct)
    {
        var ticketPriority = string.IsNullOrWhiteSpace(request.Priority)
            ? TicketPriority.Normal
            : TicketPriority.FromString(request.Priority);

        var ticketStatus = string.IsNullOrWhiteSpace(request.Status)
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