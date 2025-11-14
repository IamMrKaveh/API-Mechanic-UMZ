namespace Infrastructure.Notification;

public class NotificationService : INotificationService
{
    private readonly MechanicContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        MechanicContext context,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task CreateNotificationAsync(int userId, string title, string message, string type, string? actionUrl = null, int? relatedEntityId = null, string? relatedEntityType = null)
    {
        var notification = new Domain.Notification.Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            ActionUrl = actionUrl,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<Domain.Notification.Notification>().Add(notification);
        // Note: SaveChangesAsync is removed. It will be called by the UnitOfWork in the use case.
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Domain.Notification.Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
    {
        var query = _context.Set<Domain.Notification.Notification>().Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _context.Set<Domain.Notification.Notification>().FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notification == null || notification.IsRead) return false;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        await _context.Set<Domain.Notification.Notification>()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true).SetProperty(n => n.ReadAt, DateTime.UtcNow));
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Set<Domain.Notification.Notification>().CountAsync(n => n.UserId == userId && !n.IsRead);
    }
}