namespace Domain.Brand.Events;

public sealed class BrandDeletedEvent : DomainEvent
{
    public Guid BrandId { get; }
    public Guid CategoryId { get; }

    public BrandDeletedEvent(Guid brandId, Guid categoryId)
    {
        BrandId = brandId;
        CategoryId = categoryId;
    }
}