using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Brand.Events;

public sealed record BrandDeletedEvent(BrandId BrandId, BrandName Name, CategoryId CategoryId, UserId? DeletedBy) : DomainEvent;
