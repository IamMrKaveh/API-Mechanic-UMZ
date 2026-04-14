namespace Infrastructure.Persistence.Outbox;

public readonly record struct OutboxMessageId(Guid Value)
{
    public static OutboxMessageId NewId() => new(Guid.NewGuid());
}