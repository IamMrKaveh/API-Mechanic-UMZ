using Application.Attribute.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Attribute.Features.Queries.GetAllAttributeTypes;

[RequestOutputCache(
    tags:
    [
        CacheTags.AttributeType,
        CacheTags.AttributeValue
    ],
    expirationInSeconds: 3600)]
public record GetAllAttributeTypesQuery(
    int Page = 1,
    int PageSize = 10) : IPageQuery<AttributeTypeDto>;
