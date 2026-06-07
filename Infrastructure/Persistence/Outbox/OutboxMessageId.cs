namespace Infrastructure.Persistence.Outbox;

public readonly record struct OutboxMessageId(Guid Value)
{
    public static OutboxMessageId NewId() => new(Guid.NewGuid());

    public static OutboxMessageId From(Guid value) => value == Guid.Empty
        ? throw new NoNullAllowedException("OutboxMessageId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(OutboxMessageId id) => id.Value;
}