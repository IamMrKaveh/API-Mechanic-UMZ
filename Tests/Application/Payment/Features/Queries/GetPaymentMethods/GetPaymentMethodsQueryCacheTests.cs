using Application.Cache.Contracts;
using Application.Payment.Features.Queries.GetPaymentMethods;

namespace Tests.Application.Payment.Features.Queries.GetPaymentMethods;

public class GetPaymentMethodsQueryCacheTests
{
    [Theory]
    [InlineData(false, false, "payment-methods:list:inactive=False:deleted=False")]
    [InlineData(true, false, "payment-methods:list:inactive=True:deleted=False")]
    [InlineData(false, true, "payment-methods:list:inactive=False:deleted=True")]
    [InlineData(true, true, "payment-methods:list:inactive=True:deleted=True")]
    public void CacheKey_ContainsBothFlags(bool includeInactive, bool includeDeleted, string expected)
    {
        var query = new GetPaymentMethodsQuery(includeInactive, includeDeleted);

        ((ICacheableQuery)query).CacheKey.ShouldBe(expected);
    }

    [Fact]
    public void Expiry_IsThirtyMinutes()
    {
        var query = new GetPaymentMethodsQuery();

        ((ICacheableQuery)query).Expiry.ShouldBe(TimeSpan.FromMinutes(30));
    }
}
