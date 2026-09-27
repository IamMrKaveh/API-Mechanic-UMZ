using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Domain.Brand.Events;

public sealed record BrandDeactivatedEvent(BrandId BrandId, BrandName Name, CategoryId CategoryId) : DomainEvent;
