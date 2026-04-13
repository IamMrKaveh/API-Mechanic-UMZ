using Application.Search.Contracts;
using Application.Search.Features.Shared;

namespace Application.Search.Events;

public record ProductChangedEvent(int EntityId, EntityChangeType ChangeType, ProductSearchDocument? Document = null)
    : IEntityChangeEvent
{
    public string EntityType => "Product";
}