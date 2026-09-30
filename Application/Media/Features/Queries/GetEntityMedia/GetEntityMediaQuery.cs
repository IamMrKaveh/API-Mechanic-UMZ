using Application.Media.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Media.Features.Queries.GetEntityMedia;

[RequestOutputCache(
    tags:
    [
        CacheTags.Media
    ],
    expirationInSeconds: 300)]
public record GetEntityMediaQuery(
    string EntityType,
    Guid EntityId) : IQuery<IReadOnlyList<MediaDto>>;