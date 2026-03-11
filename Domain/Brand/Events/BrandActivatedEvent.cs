namespace Domain.Brand.Events;

public sealed class BrandActivatedEvent : DomainEvent
{
    public Guid BrandId { get; }
    public string Name { get; }
    public Guid CategoryId { get; }

    public BrandActivatedEvent(Guid brandId, string name, Guid categoryId)
    {
        BrandId = brandId;
        Name = name;
        CategoryId = categoryId;
    }
}