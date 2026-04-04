using Application.Common.Results;
using Application.Product.Features.Shared;

namespace Application.Variant.Features.Commands.AddVariant;

public record AddVariantCommand(
    int ProductId,
    string? Sku,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice,
    int Stock,
    bool IsUnlimited,
    decimal ShippingMultiplier,
    List<int> AttributeValueIds,
    List<int>? EnabledShippingMethodIds) : IRequest<ServiceResult<ProductVariantViewDto>>;