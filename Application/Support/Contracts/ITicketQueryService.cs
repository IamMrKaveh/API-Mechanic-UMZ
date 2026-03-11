using Application.Common.Models;

namespace Application.Support.Contracts;

public interface ITicketQueryService
{
    Task<int> CountOpenByUserIdAsync(int userId, CancellationToken ct = default);

    Task<int> CountAwaitingReplyAsync(CancellationToken ct = default);

    Task<bool> UserHasAccessAsync(int ticketId, int userId, CancellationToken ct = default);

    Task<PaginatedResult<TicketDto>> GetUserTicketsPagedAsync(
        int userId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<PaginatedResult<TicketDto>> GetAdminTicketsPagedAsync(
        string? status,
        string? priority,
        int? userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<TicketDetailDto?> GetTicketDetailAsync(
        int ticketId,
        CancellationToken ct = default);

    Task<IEnumerable<TicketDto>> GetOpenTicketsAsync(CancellationToken ct = default);

    Task<IEnumerable<TicketDto>> GetAwaitingReplyAsync(CancellationToken ct = default);

    Task<IEnumerable<TicketDto>> GetHighPriorityTicketsAsync(CancellationToken ct = default);
}