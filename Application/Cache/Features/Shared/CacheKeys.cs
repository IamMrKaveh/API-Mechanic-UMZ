namespace Application.Cache.Features.Shared;

public static class CacheKeys
{
    public static string Product(Guid id) => $"product:{id}";

    public static string ProductBySlug(string slug) => $"product:slug:{slug}";

    public static string ProductList(int page, int pageSize) => $"products:page:{page}:{pageSize}";

    public static string Category(Guid id) => $"category:{id}";

    public static string CategoryTree() => "categories:tree";

    public static string CategoryList() => "categories:all";

    public static string Brand(Guid id) => $"brand:{id}";

    public static string BrandList() => "brands:all";

    public static string Cart(Guid cartId) => $"cart:{cartId}";

    public static string UserCart(Guid userId) => $"cart:user:{userId}";

    public static string GuestCart(string token) => $"cart:guest:{token}";

    public static string Inventory(Guid variantId) => $"inventory:variant:{variantId}";

    public static string ShippingList() => "shippings:active";

    public static string UserProfile(Guid userId) => $"user:profile:{userId}";

    public static string DiscountCode(string code) => $"discount:code:{code}";

    public static string Wishlist(Guid userId) => $"wishlist:user:{userId}";
}