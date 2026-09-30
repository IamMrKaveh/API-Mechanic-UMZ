using Application.Location.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Location.Features.Queries.GetCities;

[RequestOutputCache(
    tags:
    [
        CacheTags.Manual.Location
    ],
    expirationInSeconds: 86400)]
public record GetCitiesQuery(int StateId) : IQuery<IEnumerable<CityDto>>;
