using Application.Cache.Features.Shared;

namespace Tests.Application.Cache.Features.Shared;

public class CacheKeysTests
{
    [Fact]
    public void Product_IncludesIdWithPrefix()
    {
        var id = Guid.NewGuid();

        CacheKeys.Product(id).ShouldBe($"product:{id}");
    }

    [Fact]
    public void Inventory_IncludesVariantIdWithPrefix()
    {
        var variantId = Guid.NewGuid();

        CacheKeys.Inventory(variantId).ShouldBe($"inventory:variant:{variantId}");
    }

    [Fact]
    public void UserProfile_IncludesUserIdWithPrefix()
    {
        var userId = Guid.NewGuid();

        CacheKeys.UserProfile(userId).ShouldBe($"user:profile:{userId}");
    }

    [Fact]
    public void Keys_ForDifferentIds_AreDistinct()
    {
        CacheKeys.Product(Guid.NewGuid()).ShouldNotBe(CacheKeys.Product(Guid.NewGuid()));
        CacheKeys.Inventory(Guid.NewGuid()).ShouldNotBe(CacheKeys.Inventory(Guid.NewGuid()));
        CacheKeys.UserProfile(Guid.NewGuid()).ShouldNotBe(CacheKeys.UserProfile(Guid.NewGuid()));
    }

    [Fact]
    public void Keys_ForDifferentKinds_DoNotCollide()
    {
        var id = Guid.NewGuid();

        CacheKeys.Product(id).ShouldNotBe(CacheKeys.Inventory(id));
        CacheKeys.Product(id).ShouldNotBe(CacheKeys.UserProfile(id));
        CacheKeys.Inventory(id).ShouldNotBe(CacheKeys.UserProfile(id));
    }

    [Fact]
    public void Keys_AreDeterministic_ForSameId()
    {
        var id = Guid.NewGuid();

        CacheKeys.Product(id).ShouldBe(CacheKeys.Product(id));
        CacheKeys.Inventory(id).ShouldBe(CacheKeys.Inventory(id));
        CacheKeys.UserProfile(id).ShouldBe(CacheKeys.UserProfile(id));
    }
}
