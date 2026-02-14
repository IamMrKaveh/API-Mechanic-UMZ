namespace Infrastructure.Notification.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly LedkaContext _context;

    public NotificationRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<Domain.Notification.Notification?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Notifications.FindAsync(new object[] { id }, ct);
    }

    public async Task<(IEnumerable<Domain.Notification.Notification> Items, int TotalCount)> GetByUserIdAsync(
        int userId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IEnumerable<Domain.Notification.Notification>> GetUnreadByUserIdAsync(
        int userId,
        int? limit = null,
        CancellationToken ct = default)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt);

        if (limit.HasValue)
        {
            return await query.Take(limit.Value).ToListAsync(ct);
        }

        return await query.ToListAsync(ct);
    }

    public async Task<int> CountUnreadByUserIdAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task<IEnumerable<Domain.Notification.Notification>> GetByRelatedEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default)
    {
        return await _context.Notifications
            .Where(n => n.RelatedEntityType == entityType && n.RelatedEntityId == entityId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Domain.Notification.Notification>> GetRecentByUserIdAsync(
        int userId,
        int count,
        CancellationToken ct = default)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<int> MarkAllAsReadByUserIdAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
    }

    public async Task<int> DeleteOldNotificationsAsync(DateTime olderThan, CancellationToken ct = default)
    {
        return await _context.Notifications
            .Where(n => n.CreatedAt < olderThan && n.IsRead)
            .ExecuteDeleteAsync(ct);
    }

    public async Task AddAsync(Domain.Notification.Notification notification, CancellationToken ct = default)
    {
        await _context.Notifications.AddAsync(notification, ct);
    }

    public void Update(Domain.Notification.Notification notification)
    {
        _context.Notifications.Update(notification);
    }
}