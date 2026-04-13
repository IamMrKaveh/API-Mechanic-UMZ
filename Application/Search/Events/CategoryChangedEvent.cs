using Application.Search.Contracts;
using Application.Search.Features.Shared;

namespace Application.Search.Events;

public record CategoryChangedEvent(int EntityId, EntityChangeType ChangeType, CategorySearchDocument? Document = null)
    : IEntityChangeEvent
{
    public string EntityType => "Category";
}