using Application.Search.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Search.Features.Queries.GlobalSearch;

[RequestOutputCache(
    tags:
    [
        CacheTags.Product,
        CacheTags.Category,
        CacheTags.Brand
    ],
    expirationInSeconds: 60)]
public record GlobalSearchQuery(
    string Q)
    : IQuery<GlobalSearchResultDto>;