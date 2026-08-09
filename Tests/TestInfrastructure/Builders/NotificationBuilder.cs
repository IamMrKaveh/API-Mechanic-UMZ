
using Bogus;
using Domain.Notification.Aggregates;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class NotificationBuilder
{
    private static readonly Faker Faker = new("en");

    private NotificationId _id = NotificationId.NewId();
    private UserId _userId = UserId.NewId();
    private NotificationType _type = NotificationType.OrderCreated;
    private string _title = Faker.Lorem.Sentence(3);
    private string _message = Faker.Lorem.Sentence(8);
    private string? _actionUrl;
    private string? _relatedEntityType;
    private Guid? _relatedEntityId;

    public NotificationBuilder WithId(NotificationId id)
    {
        _id = id;
        return this;
    }

    public NotificationBuilder WithUserId(UserId userId)
    {
        _userId = userId;
        return this;
    }

    public NotificationBuilder WithType(NotificationType type)
    {
        _type = type;
        return this;
    }

    public NotificationBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public NotificationBuilder WithMessage(string message)
    {
        _message = message;
        return this;
    }

    public NotificationBuilder WithActionUrl(string? actionUrl)
    {
        _actionUrl = actionUrl;
        return this;
    }

    public NotificationBuilder WithRelatedEntity(string? relatedEntityType, Guid? relatedEntityId)
    {
        _relatedEntityType = relatedEntityType;
        _relatedEntityId = relatedEntityId;
        return this;
    }

    public Notification Build() =>
        Notification.Create(
            _id,
            _userId,
            _type,
            _title,
            _message,
            _actionUrl,
            _relatedEntityType,
            _relatedEntityId);
}

