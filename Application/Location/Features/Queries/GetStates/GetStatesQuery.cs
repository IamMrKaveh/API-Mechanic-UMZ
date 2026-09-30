using Application.Location.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Location.Features.Queries.GetStates;

[RequestOutputCache(
    tags:
    [
        CacheTags.Manual.Location
    ],
    expirationInSeconds: 86400)]
public record GetStatesQuery(
    int Page = 1,
    int PageSize = 50) : IPageQuery<ProvinceDto>;
