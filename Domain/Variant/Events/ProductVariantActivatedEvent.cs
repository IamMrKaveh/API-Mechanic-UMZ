namespace Domain.Variant.Events;

public sealed record ProductVariantActivatedEvent(
    ProductVariantId VariantId,
    ProductId ProductId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}