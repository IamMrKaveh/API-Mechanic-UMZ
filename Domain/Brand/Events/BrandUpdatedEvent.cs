namespace Domain.Brand.Events;

public sealed class BrandUpdatedEvent : DomainEvent
{
    public Guid BrandId { get; }
    public string Name { get; }
    public string Slug { get; }
    public string? Description { get; }

    public BrandUpdatedEvent(Guid brandId, string name, string slug, string? description)
    {
        BrandId = brandId;
        Name = name;
        Slug = slug;
        Description = description;
    }
}