using Application.Search.Contracts;
using Application.Search.Features.Shared;

namespace Application.Search.Events;

public record BrandChangedEvent(int EntityId, EntityChangeType ChangeType, BrandSearchDocument? Document = null)
    : IEntityChangeEvent
{
    public string EntityType => "Brand";
}