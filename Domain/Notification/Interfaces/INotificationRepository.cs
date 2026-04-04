namespace Domain.Notification.Interfaces;

public interface INotificationRepository
{
    Task<Aggregates.Notification?> GetByIdAsync(
        int id,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Notification>> GetUnreadByUserIdAsync(
        int userId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Notification>> GetByUserIdAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Notification>> GetReadNotificationsOlderThanAsync(
        DateTime cutoff,
        CancellationToken ct = default);

    Task AddAsync(
        Aggregates.Notification notification,
        CancellationToken ct = default);

    void Update(Aggregates.Notification notification);

    void Remove(Aggregates.Notification notification);
}