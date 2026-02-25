namespace Infrastructure.Audit.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly DBContext _context;

    public AuditRepository(DBContext context)
    {
        _context = context;
    }

    public async Task AddAuditLogAsync(AuditLog log)
    {
        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task<(IEnumerable<AuditLog> Logs, int Total)> GetAuditLogsAsync(
        DateTime? from,
        DateTime? to,
        int? userId,
        string? type,
        int page,
        int size)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value);

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(l => l.EventType == type);

        var total = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (logs, total);
    }

    public async Task<(IEnumerable<AuditDtos> Logs, int Total)> SearchAsync(
        AuditSearchRequest request,
        CancellationToken ct = default)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(l => l.UserId == request.UserId.Value);

        if (!string.IsNullOrEmpty(request.EventType))
            query = query.Where(l => l.EventType == request.EventType);

        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(l => l.Action.Contains(request.Action));

        if (!string.IsNullOrEmpty(request.IpAddress))
            query = query.Where(l => l.IpAddress == request.IpAddress);

        if (request.From.HasValue)
            query = query.Where(l => l.Timestamp >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(l => l.Timestamp <= request.To.Value);

        if (!string.IsNullOrEmpty(request.Keyword))
            query = query.Where(l => l.Details.Contains(request.Keyword) || l.Action.Contains(request.Keyword));

        query = request.SortDesc
            ? query.OrderByDescending(l => l.Timestamp)
            : query.OrderBy(l => l.Timestamp);

        var total = await query.CountAsync(ct);

        var logs = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new AuditDtos
            {
                Id = l.Id,
                UserId = l.UserId,
                EventType = l.EventType,
                Action = l.Action,
                Details = l.Details,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                Timestamp = l.Timestamp,
                IsArchived = l.IsArchived
            })
            .ToListAsync(ct);

        return (logs, total);
    }
}