using Application.Support.Features.Shared;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Support.Contracts;

public interface ITicketQueryService
{
    Task<PaginatedResult<TicketDto>> GetAdminTicketsPagedAsync(
        TicketStatus ticketStatus,
        TicketPriority ticketPriority,
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<TicketDto?> GetTicketDetailAsync(
        TicketId ticketId,
        CancellationToken ct = default);

    Task<PaginatedResult<TicketListItemDto>> GetTicketsPagedAsync(
        UserId? userId,
        TicketStatus? status,
        TicketPriority? priority,
        int page,
        int pageSize,
        CancellationToken ct = default);
}