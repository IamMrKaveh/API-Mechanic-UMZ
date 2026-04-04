using Application.Common.Results;

namespace Application.Support.Features.Queries.GetUserTickets;

public sealed class GetUserTicketsHandler(ITicketQueryService ticketQueryService)
        : IRequestHandler<GetUserTicketsQuery, ServiceResult<PaginatedResult<TicketDto>>>
{
    private readonly ITicketQueryService _ticketQueryService = ticketQueryService;

    public async Task<ServiceResult<PaginatedResult<TicketDto>>> Handle(
        GetUserTicketsQuery request,
        CancellationToken ct)
    {
        var result = await _ticketQueryService.GetUserTicketsPagedAsync(
            request.UserId,
            request.Status,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<TicketDto>>.Success(result);
    }
}