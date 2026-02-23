namespace Infrastructure.Support.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly Persistence.Context.DBContext _context;

    public TicketRepository(Persistence.Context.DBContext context)
    {
        _context = context;
    }

    public async Task<Ticket?> GetByIdWithMessagesAsync(int id, CancellationToken ct = default)
    {
        return await _context.Tickets
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<(IEnumerable<Ticket> Items, int TotalCount)> GetByUserIdAsync(
        int userId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Tickets
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IEnumerable<Ticket>> GetOpenTicketsAsync(CancellationToken ct = default)
    {
        return await _context.Tickets
            .Where(t => t.Status == Ticket.TicketStatuses.Open)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Ticket>> GetAwaitingReplyAsync(CancellationToken ct = default)
    {
        return await _context.Tickets
            .Where(t => t.Status == Ticket.TicketStatuses.AwaitingReply)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Ticket>> GetHighPriorityTicketsAsync(CancellationToken ct = default)
    {
        return await _context.Tickets
            .Where(t =>
                (t.Priority == Ticket.TicketPriorities.High || t.Priority == Ticket.TicketPriorities.Urgent)
                && t.Status != Ticket.TicketStatuses.Closed)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<(IEnumerable<Ticket> Items, int TotalCount)> GetAdminTicketsAsync(
        string? status,
        string? priority,
        int? userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Tickets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(t => t.Priority == priority);

        if (userId.HasValue)
            query = query.Where(t => t.UserId == userId.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<int> CountOpenByUserIdAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Tickets
            .CountAsync(t => t.UserId == userId && t.Status != Ticket.TicketStatuses.Closed, ct);
    }

    public async Task<int> CountAwaitingReplyAsync(CancellationToken ct = default)
    {
        return await _context.Tickets
            .CountAsync(t => t.Status == Ticket.TicketStatuses.AwaitingReply, ct);
    }

    public async Task<bool> UserHasAccessAsync(int ticketId, int userId, CancellationToken ct = default)
    {
        return await _context.Tickets
            .AnyAsync(t => t.Id == ticketId && t.UserId == userId, ct);
    }

    public async Task AddAsync(Ticket ticket, CancellationToken ct = default)
    {
        await _context.Tickets.AddAsync(ticket, ct);
    }

    public void Update(Ticket ticket)
    {
        _context.Tickets.Update(ticket);
    }
}