namespace Domain.Common.Events;

/// <summary>
/// کلاس پایه برای Domain Event‌ها
/// باید INotification از MediatR را پیاده‌سازی کند
/// </summary>
[NotMapped]
public abstract class DomainEvent : INotification
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}