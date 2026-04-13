namespace Application.Search.Contracts;

public interface IEntityChangeEvent
{
    int EntityId { get; }
    string EntityType { get; }
    EntityChangeType ChangeType { get; }
}