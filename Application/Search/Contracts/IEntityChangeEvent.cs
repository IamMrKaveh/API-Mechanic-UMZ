namespace Application.Search.Contracts;

/// <summary>
/// Interface for entity change events
/// </summary>
public interface IEntityChangeEvent
{
    int EntityId { get; }
    string EntityType { get; }
    EntityChangeType ChangeType { get; }
}