namespace Application.Cache.Features.Shared;

/// <summary>
/// کلیدهای استاندارد Cache.
/// تمرکز کلیدها در یک مکان از بروز کلیدهای اشتباه جلوگیری می‌کند.
/// </summary>
public static class CacheKeys
{
    
    public static string Product(int id) => $"product:{id}";

    public static string ProductList(string hash) => $"products:list:{hash}";

    public static string ProductSearch(string q) => $"products:search:{q}";

    
    public static string Category(int id) => $"category:{id}";

    public static string CategoryTree() => "categories:tree";

    public static string CategoryProducts(int id) => $"category:{id}:products";

    
    public static string Inventory(int variantId) => $"inventory:{variantId}";

    public static string InventoryStatus(int vid) => $"inventory:status:{vid}";

    
    public static string Cart(string cartKey) => $"cart:{cartKey}";

    public static string UserCart(int userId) => $"cart:user:{userId}";

    
    public static string UserOrders(int userId) => $"orders:user:{userId}";

    public static string Order(int id) => $"order:{id}";

    
    public static string ProductPrefix(int id) => $"product:{id}:*";

    public static string CategoryPrefix(int id) => $"category:{id}:*";

    public static string InventoryPrefix() => "inventory:*";
}