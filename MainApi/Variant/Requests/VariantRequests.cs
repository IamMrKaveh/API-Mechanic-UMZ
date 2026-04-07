namespace Presentation.Variant.Requests;

public record AddVariantRequest(
    string Sku,
    decimal Price,
    decimal? CompareAtPrice = null,
    IReadOnlyList<AttributeAssignmentRequest>? Attributes = null
);

public record UpdateVariantRequest(
    string Sku,
    decimal Price,
    decimal? CompareAtPrice = null,
    IReadOnlyList<AttributeAssignmentRequest>? Attributes = null,
    string? RowVersion = null
);

public record AttributeAssignmentRequest(
    Guid AttributeTypeId,
    Guid AttributeValueId,
    string DisplayValue
);

public record AddStockRequest(int Quantity, string Reason);

public record RemoveStockRequest(int Quantity, string Reason);

public record UpdateVariantShippingRequest(
    IReadOnlyList<VariantShippingItemRequest> ShippingMethods
);

public record VariantShippingItemRequest(
    Guid ShippingId,
    decimal Weight,
    decimal Width,
    decimal Height,
    decimal Length
);