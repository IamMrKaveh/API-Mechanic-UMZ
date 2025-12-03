namespace Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly LedkaContext _context;

    public NotificationRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<Domain.Notification.Notification?> GetByIdAsync(int id)
    {
        return await _context.Notifications.FindAsync(id);
    }

    public async Task<IEnumerable<Domain.Notification.Notification>> GetByUserIdAsync(int userId, bool unreadOnly = false, int page = 1, int pageSize = 20)
    {
        var query = _context.Notifications.Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task AddAsync(Domain.Notification.Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
    }

    public void Update(Domain.Notification.Notification notification)
    {
        _context.Notifications.Update(notification);
    }

    public void Delete(Domain.Notification.Notification notification)
    {
        _context.Notifications.Remove(notification);
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow));
    }
}