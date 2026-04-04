using Application.Common.Results;

namespace Application.Support.Features.Queries.GetAdminTickets;

public sealed class GetAdminTicketsHandler(ITicketQueryService ticketQueryService)
        : IRequestHandler<GetAdminTicketsQuery, ServiceResult<PaginatedResult<TicketDto>>>
{
    private readonly ITicketQueryService _ticketQueryService = ticketQueryService;

    public async Task<ServiceResult<PaginatedResult<TicketDto>>> Handle(
        GetAdminTicketsQuery request,
        CancellationToken ct)
    {
        var result = await _ticketQueryService.GetAdminTicketsPagedAsync(
            request.Status,
            request.Priority,
            request.UserId,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<TicketDto>>.Success(result);
    }
}