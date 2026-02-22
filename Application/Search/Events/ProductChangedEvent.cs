namespace Application.Search.Events;

/// <summary>
/// Product change event
/// </summary>
public record ProductChangedEvent(int EntityId, EntityChangeType ChangeType, ProductSearchDocument? Document = null)
    : IEntityChangeEvent
{
    public string EntityType => "Product";
}