namespace Application.Search.Events;

/// <summary>
/// Brand change event
/// </summary>
public record BrandChangedEvent(int EntityId, EntityChangeType ChangeType, BrandSearchDocument? Document = null)
    : IEntityChangeEvent
{
    public string EntityType => "Brand";
}