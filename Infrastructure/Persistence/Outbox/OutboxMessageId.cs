namespace Infrastructure.Persistence.Outbox;

public sealed record OutboxMessageId(Guid Value)
{
    public static OutboxMessageId NewId() => new(Guid.NewGuid());
    public static OutboxMessageId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}