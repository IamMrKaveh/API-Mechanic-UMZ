namespace Application.Support.Features.Queries.GetAdminTickets;

public sealed class GetAdminTicketsHandler
    : IRequestHandler<GetAdminTicketsQuery, ServiceResult<PaginatedResult<TicketDto>>>
{
    private readonly ITicketRepository _ticketRepository;

    public GetAdminTicketsHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<ServiceResult<PaginatedResult<TicketDto>>> Handle(
        GetAdminTicketsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _ticketRepository.GetAdminTicketsAsync(
            request.Status,
            request.Priority,
            request.UserId,
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