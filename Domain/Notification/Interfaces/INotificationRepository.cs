namespace Domain.Notification.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(int userId, CancellationToken ct = default);

    Task<IReadOnlyList<Notification>> GetByUserIdAsync(int userId, int page, int pageSize, CancellationToken ct = default);

    Task<IReadOnlyList<Notification>> GetReadNotificationsOlderThanAsync(DateTime cutoff, CancellationToken ct = default);

    Task AddAsync(Notification notification, CancellationToken ct = default);

    void Update(Notification notification);

    void Remove(Notification notification);
}