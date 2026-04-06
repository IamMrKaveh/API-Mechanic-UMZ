using Domain.Common.Abstractions;

namespace Application.Common.Events;

public record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;