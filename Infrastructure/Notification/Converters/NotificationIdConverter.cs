using Domain.Notification.ValueObjects;

namespace Infrastructure.Notification.Converters;

internal sealed class NotificationIdConverter : StronglyTypedIdConverter<NotificationId>
{
    public NotificationIdConverter() : base(NotificationId.From)
    {
    }
}