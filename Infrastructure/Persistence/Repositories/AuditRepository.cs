namespace Infrastructure.Persistence.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly LedkaContext _context;

    public AuditRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task AddAuditLogAsync(Domain.Log.AuditLog auditLog)
    {
        await _context.Set<Domain.Log.AuditLog>().AddAsync(auditLog);
    }

    public async Task<(IEnumerable<Domain.Log.AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(
        DateTime? fromDate = null, DateTime? toDate = null, int? userId = null, string? eventType = null, int page = 1, int pageSize = 50)
    {
        var query = _context.Set<Domain.Log.AuditLog>().AsNoTracking().AsQueryable();
        if (fromDate.HasValue)
            query = query.Where(log => log.Timestamp >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(log => log.Timestamp <= toDate.Value);
        if (userId.HasValue)
            query = query.Where(log => log.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(log => log.EventType == eventType);

        var totalCount = await query.CountAsync();

        var logs = await query
                    .OrderByDescending(log => log.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

        return (logs, totalCount);
    }
}