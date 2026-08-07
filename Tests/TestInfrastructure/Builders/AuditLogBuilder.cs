using Domain.Audit.Entities;
using Domain.User.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class AuditLogBuilder
{
    private static readonly Faker Faker = new();

    private UserId? _userId;
    private string _eventType = "Security";
    private string _action = "Login";
    private string _ipAddress = "127.0.0.1";
    private string? _entityType;
    private string? _entityId;
    private string? _details;
    private string? _userAgent;

    public AuditLogBuilder WithUserId(UserId? userId)
    {
        _userId = userId;
        return this;
    }

    public AuditLogBuilder WithRandomUser()
    {
        _userId = UserId.NewId();
        return this;
    }

    public AuditLogBuilder WithEventType(string eventType)
    {
        _eventType = eventType;
        return this;
    }

    public AuditLogBuilder WithAction(string action)
    {
        _action = action;
        return this;
    }

    public AuditLogBuilder WithIpAddress(string ipAddress)
    {
        _ipAddress = ipAddress;
        return this;
    }

    public AuditLogBuilder WithEntityType(string? entityType)
    {
        _entityType = entityType;
        return this;
    }

    public AuditLogBuilder WithEntityId(string? entityId)
    {
        _entityId = entityId;
        return this;
    }

    public AuditLogBuilder WithDetails(string? details)
    {
        _details = details;
        return this;
    }

    public AuditLogBuilder WithUserAgent(string? userAgent)
    {
        _userAgent = userAgent;
        return this;
    }

    public AuditLogBuilder WithRandomOrderEvent()
    {
        _eventType = "Order";
        _action = Faker.PickRandom("Created", "Paid", "Shipped", "Cancelled");
        _entityType = "Order";
        _entityId = Guid.NewGuid().ToString();
        return this;
    }

    public AuditLog Build() =>
        AuditLog.Create(
            _userId,
            _eventType,
            _action,
            _ipAddress,
            _entityType,
            _entityId,
            _details,
            _userAgent);
}
