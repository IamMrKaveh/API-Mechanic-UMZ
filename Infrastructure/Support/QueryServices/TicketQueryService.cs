namespace Infrastructure.Support.QueryServices;

public class TicketQueryService(DBContext context) : ITicketQueryService
{
    private readonly DBContext _context = context;

    public async Task<int> CountOpenByUserIdAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Tickets
            .AsNoTracking()
            .CountAsync(t => t.UserId == userId && t.Status == "Open", ct);
    }

    public async Task<int> CountAwaitingReplyAsync(CancellationToken ct = default)
    {
        return await _context.Tickets
            .AsNoTracking()
            .CountAsync(t => t.Status == "AwaitingReply", ct);
    }

    public async Task<bool> UserHasAccessAsync(int ticketId, int userId, CancellationToken ct = default)
    {
        return await _context.Tickets
            .AsNoTracking()
            .AnyAsync(t => t.Id == ticketId && t.UserId == userId, ct);
    }

    public async Task<PaginatedResult<TicketDto>> GetUserTicketsPagedAsync(
        int userId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Tickets
            .AsNoTracking()
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketDto
            {
                Id = t.Id,
                UserId = t.UserId,
                Subject = t.Subject,
                Status = t.Status,
                Priority = t.Priority,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(ct);

        return PaginatedResult<TicketDto>.Create(items, total, page, pageSize);
    }

    public async Task<PaginatedResult<TicketDto>> GetAdminTicketsPagedAsync(
        string? status,
        string? priority,
        int? userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Tickets.AsNoTracking();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrEmpty(priority))
            query = query.Where(t => t.Priority == priority);

        if (userId.HasValue)
            query = query.Where(t => t.UserId == userId.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketDto
            {
                Id = t.Id,
                UserId = t.UserId,
                Subject = t.Subject,
                Status = t.Status,
                Priority = t.Priority,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(ct);

        return PaginatedResult<TicketDto>.Create(items, total, page, pageSize);
    }

    public async Task<TicketDetailDto?> GetTicketDetailAsync(int ticketId, CancellationToken ct = default)
    {
        var ticket = await _context.Tickets
            .AsNoTracking()
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket == null) return null;

        return new TicketDetailDto
        {
            Id = ticket.Id,
            UserId = ticket.UserId,
            Subject = ticket.Subject,
            Status = ticket.Status,
            Priority = ticket.Priority,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            Messages = ticket.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new TicketMessageDto
                {
                    Id = m.Id,
                    Message = m.Message,
                    IsAdminResponse = m.IsAdminResponse,
                    CreatedAt = m.CreatedAt
                })
                .ToList()
        };
    }

    public async Task<IEnumerable<TicketDto>> GetOpenTicketsAsync(CancellationToken ct = default)
    {
        return await _context.Tickets
            .AsNoTracking()
            .Where(t => t.Status == "Open")
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TicketDto
            {
                Id = t.Id,
                UserId = t.UserId,
                Subject = t.Subject,
                Status = t.Status,
                Priority = t.Priority,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<TicketDto>> GetAwaitingReplyAsync(CancellationToken ct = default)
    {
        return await _context.Tickets
            .AsNoTracking()
            .Where(t => t.Status == "AwaitingReply")
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TicketDto
            {
                Id = t.Id,
                UserId = t.UserId,
                Subject = t.Subject,
                Status = t.Status,
                Priority = t.Priority,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<TicketDto>> GetHighPriorityTicketsAsync(CancellationToken ct = default)
    {
        return await _context.Tickets
            .AsNoTracking()
            .Where(t => t.Priority == "High" || t.Priority == "Urgent")
            .Where(t => t.Status != "Closed")
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TicketDto
            {
                Id = t.Id,
                UserId = t.UserId,
                Subject = t.Subject,
                Status = t.Status,
                Priority = t.Priority,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(ct);
    }
}