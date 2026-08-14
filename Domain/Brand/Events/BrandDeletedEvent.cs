using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Brand.Events;

public sealed class BrandDeletedEvent(BrandId brandId, BrandName name, CategoryId categoryId, UserId? deletedBy) : DomainEvent
{
    public BrandId BrandId { get; } = brandId;
    public BrandName Name { get; } = name;
    public CategoryId CategoryId { get; } = categoryId;
    public UserId? DeletedBy { get; } = deletedBy;
}
