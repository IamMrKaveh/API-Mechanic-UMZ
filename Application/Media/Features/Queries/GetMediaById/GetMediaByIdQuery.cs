using Application.Media.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Media.Features.Queries.GetMediaById;

[RequestOutputCache(
    tags:
    [
        CacheTags.Media
    ],
    expirationInSeconds: 300)]
public record GetMediaByIdQuery(
    Guid MediaId) : IQuery<MediaDto>;