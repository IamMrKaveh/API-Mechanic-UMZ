namespace MainApi.Services.Notification;

public class NotificationService : INotificationService
{
    private readonly MechanicContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(MechanicContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CreateNotificationAsync(int userId, string title, string message, string type, string? actionUrl = null, int? relatedEntityId = null, string? relatedEntityType = null)
    {
        var notification = new TNotification
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

        _context.TNotification.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<TNotification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
    {
        var query = _context.TNotification.Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _context.TNotification.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notification == null || notification.IsRead) return false;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.TNotification.CountAsync(n => n.UserId == userId && !n.IsRead);
    }
}