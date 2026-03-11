using Domain.Common.Abstractions;
using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed record ProductActivatedEvent(ProductId ProductId) : IDomainEvent;