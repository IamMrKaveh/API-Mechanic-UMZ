using Application.Cache.Contracts;
using Application.Shipping.Features.Queries.GetShippings;

namespace Tests.Application.Shipping.Features.Queries.GetShippings;

public class GetShippingsQueryCacheTests
{
    [Theory]
    [InlineData(false, "shippings:list:inactive=False")]
    [InlineData(true, "shippings:list:inactive=True")]
    public void CacheKey_ContainsIncludeInactiveFlag(bool includeInactive, string expected)
    {
        var query = new GetShippingsQuery(includeInactive);

        ((ICacheableQuery)query).CacheKey.ShouldBe(expected);
    }

    [Fact]
    public void Expiry_IsThirtyMinutes()
    {
        var query = new GetShippingsQuery();

        ((ICacheableQuery)query).Expiry.ShouldBe(TimeSpan.FromMinutes(30));
    }
}
