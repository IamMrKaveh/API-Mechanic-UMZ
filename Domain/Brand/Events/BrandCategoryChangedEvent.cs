using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Domain.Brand.Events;

public sealed record BrandCategoryChangedEvent(BrandId BrandId, CategoryId PreviousCategoryId, CategoryId NewCategoryId) : DomainEvent;
