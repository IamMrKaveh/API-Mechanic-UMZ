namespace Domain.Brand.Events;

public sealed class BrandCategoryChangedEvent : DomainEvent
{
    public Guid BrandId { get; }
    public Guid PreviousCategoryId { get; }
    public Guid NewCategoryId { get; }

    public BrandCategoryChangedEvent(Guid brandId, Guid previousCategoryId, Guid newCategoryId)
    {
        BrandId = brandId;
        PreviousCategoryId = previousCategoryId;
        NewCategoryId = newCategoryId;
    }
}