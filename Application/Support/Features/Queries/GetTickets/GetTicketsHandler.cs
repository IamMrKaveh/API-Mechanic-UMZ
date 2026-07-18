using Application.Support.Features.Shared;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Support.Features.Queries.GetTickets;

public class GetTicketsHandler(
    ITicketQueryService supportQueryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetTicketsQuery, PaginatedResult<TicketListItemDto>>
{
    public async Task<ServiceResult<PaginatedResult<TicketListItemDto>>> Handle(
        GetTicketsQuery request,
        CancellationToken ct)
    {
        TicketPriority? ticketPriority = string.IsNullOrWhiteSpace(request.Priority)
            ? null
            : TicketPriority.FromString(request.Priority);

        TicketStatus? ticketStatus = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : TicketStatus.FromString(request.Status);

        var userId = UserId.From(currentUserService.UserId.Value);

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