namespace Domain.Common.Events;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}