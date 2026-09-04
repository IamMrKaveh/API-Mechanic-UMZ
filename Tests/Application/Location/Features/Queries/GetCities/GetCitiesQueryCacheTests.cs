using Application.Cache.Contracts;
using Application.Location.Features.Queries.GetCities;

namespace Tests.Application.Location.Features.Queries.GetCities;

public class GetCitiesQueryCacheTests
{
    [Fact]
    public void CacheKey_ContainsStateId()
    {
        var query = new GetCitiesQuery(StateId: 9);

        ((ICacheableQuery)query).CacheKey.ShouldBe("location:cities:state=9");
    }

    [Fact]
    public void CacheKey_DiffersPerState()
    {
        var first = new GetCitiesQuery(StateId: 9);
        var second = new GetCitiesQuery(StateId: 10);

        ((ICacheableQuery)first).CacheKey.ShouldNotBe(((ICacheableQuery)second).CacheKey);
    }

    [Fact]
    public void Expiry_IsTwentyFourHours()
    {
        var query = new GetCitiesQuery(StateId: 9);

        ((ICacheableQuery)query).Expiry.ShouldBe(TimeSpan.FromHours(24));
    }
}
