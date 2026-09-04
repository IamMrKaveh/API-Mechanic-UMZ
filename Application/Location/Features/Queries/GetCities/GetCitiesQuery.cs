using Application.Location.Features.Shared;

namespace Application.Location.Features.Queries.GetCities;

public record GetCitiesQuery(int StateId) : IQuery<IEnumerable<CityDto>>, ICacheableQuery
{
    public string CacheKey => $"location:cities:state={StateId}";

    public TimeSpan? Expiry => TimeSpan.FromHours(24);
}