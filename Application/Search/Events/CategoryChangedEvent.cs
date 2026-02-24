namespace Application.Search.Events;

/// <summary>
/// Category change event
/// </summary>
public record CategoryChangedEvent(int EntityId, EntityChangeType ChangeType, CategorySearchDocument? Document = null)
    : IEntityChangeEvent
{
    public string EntityType => "Category";
}