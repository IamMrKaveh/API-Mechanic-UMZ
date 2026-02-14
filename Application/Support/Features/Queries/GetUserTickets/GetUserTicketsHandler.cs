namespace Application.Support.Features.Queries.GetUserTickets;

public sealed class GetUserTicketsHandler
    : IRequestHandler<GetUserTicketsQuery, ServiceResult<PaginatedResult<TicketDto>>>
{
    private readonly ITicketRepository _ticketRepository;

    public GetUserTicketsHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<ServiceResult<PaginatedResult<TicketDto>>> Handle(
        GetUserTicketsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _ticketRepository.GetByUserIdAsync(
            request.UserId,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(t => new TicketDto
        {
            Id = t.Id,
            Subject = t.Subject,
            Status = t.Status,
            Priority = t.Priority,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        })
            .ToList();

        var result = PaginatedResult<TicketDto>.Create(
            dtos,
            totalCount,
            request.Page,
            request.PageSize);

        return ServiceResult<PaginatedResult<TicketDto>>.Success(result);
    }
}