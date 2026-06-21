using Application.Common.Interfaces;
using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetProductInventoryStatuses;

public record GetProductInventoryStatusesQuery(Guid ProductId)
    : IQuery<IReadOnlyList<InventoryStatusDto>>;