using Application.Variant.Features.Shared;

namespace Application.Variant.Features.Commands.AddVariant;

public sealed record AddVariantCommand(
    Guid ProductId,
    string? Sku,
    decimal SellingPrice,
    decimal OriginalPrice,
    int Stock,
    bool IsUnlimited,
    decimal ShippingMultiplier,
    ICollection<Guid> AttributeValueIds,
    ICollection<Guid>? EnabledShippingIds)
    : ICommand<ProductVariantViewDto>, IBypassTransactionBehavior;