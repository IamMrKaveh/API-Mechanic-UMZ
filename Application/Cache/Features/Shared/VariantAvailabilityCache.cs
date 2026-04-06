namespace Application.Cache.Features.Shared;

public record VariantAvailabilityCache(
    Guid VariantId,
    int AvailableQuantity,
    bool IsUnlimited,
    bool IsInStock,
    bool IsLowStock,
    DateTime CachedAt);