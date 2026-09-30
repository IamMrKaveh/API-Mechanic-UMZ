using Application.Inventory.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Inventory.Features.Queries.GetWarehouseById;

[RequestOutputCache(
    tags:
    [
        CacheTags.Warehouse
    ],
    expirationInSeconds: 300)]
public record GetWarehouseByIdQuery(Guid Id) : IQuery<WarehouseDto>;