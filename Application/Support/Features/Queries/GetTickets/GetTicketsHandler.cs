using Application.Support.Features.Shared;

namespace Application.Support.Features.Queries.GetTickets;

public class GetTicketsHandler(
    ISupportQueryService supportQueryService) : IRequestHandler<GetTicketsQuery, ServiceResult<PaginatedResult<TicketListItemDto>>>
{
    private readonly ISupportQueryService _supportQueryService = supportQueryService;

    public async Task<ServiceResult<PaginatedResult<TicketListItemDto>>> Handle(
        GetTicketsQuery request,
        CancellationToken ct)
    {
        var result = await _supportQueryService.GetTicketsPagedAsync(
            request.UserId,
            request.Status,
            request.Priority,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<TicketListItemDto>>.Success(result);
    }
}