namespace Infrastructure.Persistence.Interface.Notification;

public interface INotificationRepository
{
    Task<Domain.Notification.Notification?> GetByIdAsync(int id);
    Task<IEnumerable<Domain.Notification.Notification>> GetByUserIdAsync(int userId, bool unreadOnly = false, int page = 1, int pageSize = 20);
    Task<int> GetUnreadCountAsync(int userId);
    Task AddAsync(Domain.Notification.Notification notification);
    void Update(Domain.Notification.Notification notification);
    void Delete(Domain.Notification.Notification notification);
    Task MarkAllAsReadAsync(int userId);
}