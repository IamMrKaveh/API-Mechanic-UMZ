namespace Application.Search.Events;

/// <summary>
/// CategoryGroup change event
/// </summary>
public record CategoryGroupChangedEvent(int EntityId, EntityChangeType ChangeType, CategoryGroupSearchDocument? Document = null)
    : IEntityChangeEvent
{
    public string EntityType => "CategoryGroup";
}
