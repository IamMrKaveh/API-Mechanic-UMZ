namespace Application.Cache.Features.Shared;

public static class CacheKeys
{
    public static string Product(Guid id) => $"product:{id}";

    public static string Inventory(Guid variantId) => $"inventory:variant:{variantId}";

    public static string UserProfile(Guid userId) => $"user:profile:{userId}";
}