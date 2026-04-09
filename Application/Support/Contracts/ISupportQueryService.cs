using Application.Support.Features.Shared;

namespace Application.Support.Contracts;

public interface ISupportQueryService
{
    Task<TicketDto?> GetTicketDetailAsync(int ticketId, CancellationToken ct = default);

    Task<PaginatedResult<TicketListItemDto>> GetTicketsPagedAsync(
        int? userId,
        string? status,
        string? priority,
        int page,
        int pageSize,
        CancellationToken ct = default);
}