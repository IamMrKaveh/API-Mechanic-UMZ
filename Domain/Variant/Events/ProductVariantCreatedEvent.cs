namespace Domain.Variant.Events;

public sealed record ProductVariantCreatedEvent(
    ProductVariantId VariantId,
    ProductId ProductId,
    string Sku,
    Money Price) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}