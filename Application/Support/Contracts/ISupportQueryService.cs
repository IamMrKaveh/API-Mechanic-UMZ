using Application.Support.Features.Shared;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Support.Contracts;

public interface ISupportQueryService
{
    Task<TicketDto?> GetTicketDetailAsync(
        TicketId ticketId,
        CancellationToken ct = default);

    Task<PaginatedResult<TicketListItemDto>> GetTicketsPagedAsync(
        UserId? userId,
        string? status,
        string? priority,
        int page,
        int pageSize,
        CancellationToken ct = default);
}