using Application.Product.Features.Shared;

namespace Application.Variant.Features.Commands.AddVariant;

public record AddVariantCommand(
    Guid ProductId,
    string? Sku,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice,
    int Stock,
    bool IsUnlimited,
    decimal ShippingMultiplier,
    ICollection<Guid> AttributeValueIds,
    ICollection<Guid>? EnabledShippingIds) : IRequest<ServiceResult<ProductVariantViewDto>>;