using Application.Brand.Features.Queries.GetPublicBrands;
using Application.Cache.Contracts;

namespace Tests.Application.Brand.Features.Queries.GetPublicBrands;

public class GetPublicBrandsQueryCacheTests
{
    [Fact]
    public void CacheKey_WhenCategoryIdIsNull_UsesAll()
    {
        var query = new GetPublicBrandsQuery(null);

        ((ICacheableQuery)query).CacheKey.ShouldBe("brands:public:category=all");
    }

    [Fact]
    public void CacheKey_WhenCategoryIdIsSet_ContainsCategoryId()
    {
        var categoryId = Guid.NewGuid();
        var query = new GetPublicBrandsQuery(categoryId);

        ((ICacheableQuery)query).CacheKey.ShouldBe($"brands:public:category={categoryId}");
    }

    [Fact]
    public void CacheKey_DiffersPerCategory()
    {
        var first = new GetPublicBrandsQuery(Guid.NewGuid());
        var second = new GetPublicBrandsQuery(Guid.NewGuid());

        ((ICacheableQuery)first).CacheKey.ShouldNotBe(((ICacheableQuery)second).CacheKey);
    }

    [Fact]
    public void Expiry_IsThirtyMinutes()
    {
        var query = new GetPublicBrandsQuery(null);

        ((ICacheableQuery)query).Expiry.ShouldBe(TimeSpan.FromMinutes(30));
    }
}
