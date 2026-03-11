using Domain.Common.Abstractions;

namespace Application.Common.Events;

public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;