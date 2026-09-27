using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed record ProductCategoryChangedEvent(
    ProductId ProductId,
    CategoryId PreviousCategoryId,
    CategoryId NewCategoryId) : DomainEvent;
