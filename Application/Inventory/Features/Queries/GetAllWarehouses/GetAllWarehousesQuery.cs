using Application.Inventory.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Inventory.Features.Queries.GetAllWarehouses;

[RequestOutputCache(
    tags:
    [
        CacheTags.Warehouse
    ],
    expirationInSeconds: 3600)]
public record GetAllWarehousesQuery : IQuery<IReadOnlyList<WarehouseDto>>;
