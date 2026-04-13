namespace Presentation.Variant.Requests;

public record AddVariantRequest(
    string? Sku,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice,
    int Stock = 0,
    bool IsUnlimited = false,
    decimal ShippingMultiplier = 1,
    ICollection<Guid>? AttributeValueIds = null,
    ICollection<Guid>? EnabledShippingIds = null
);

public record UpdateVariantRequest(
    Guid ProductId,
    Guid VariantId,
    Guid UserId,
    string? Sku,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice,
    int Stock = 0,
    bool IsUnlimited = false,
    decimal ShippingMultiplier = 1,
    ICollection<Guid>? AttributeValueIds = null,
    ICollection<Guid>? EnabledShippingIds = null
);

public record AddStockRequest(int Quantity, string Notes);

public record RemoveStockRequest(int Quantity, string Notes);

public record UpdateVariantShippingRequest(
    decimal ShippingMultiplier,
    ICollection<Guid> EnabledShippingIds
);