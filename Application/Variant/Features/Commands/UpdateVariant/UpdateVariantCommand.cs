namespace Application.Variant.Features.Commands.UpdateVariant;

public record UpdateVariantCommand(
    Guid ProductId,
    Guid VariantId,
    string? Sku,
    decimal SellingPrice,
    decimal OriginalPrice,
    int Stock,
    bool IsUnlimited,
    decimal ShippingMultiplier,
    ICollection<Guid>? AttributeValueIds,
    ICollection<Guid>? EnabledShippingIds) : IRequest<ServiceResult>;