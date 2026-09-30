using Application.Media.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Media.Features.Queries.GetAllMedia;

[RequestOutputCache(
    tags:
    [
        CacheTags.Media
    ],
    expirationInSeconds: 60)]
public record GetAllMediaQuery(
    string? EntityType,
    int Page = 1,
    int PageSize = 10) : IPageQuery<MediaDto>;