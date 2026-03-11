namespace Domain.Brand.Events;

public sealed class BrandCreatedEvent : DomainEvent
{
    public Guid BrandId { get; }
    public string Name { get; }
    public string Slug { get; }
    public Guid CategoryId { get; }

    public BrandCreatedEvent(Guid brandId, string name, string slug, Guid categoryId)
    {
        BrandId = brandId;
        Name = name;
        Slug = slug;
        CategoryId = categoryId;
    }
}