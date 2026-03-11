namespace Domain.Brand.Events;

public sealed class BrandDeactivatedEvent : DomainEvent
{
    public Guid BrandId { get; }
    public string Name { get; }
    public Guid CategoryId { get; }

    public BrandDeactivatedEvent(Guid brandId, string name, Guid categoryId)
    {
        BrandId = brandId;
        Name = name;
        CategoryId = categoryId;
    }
}