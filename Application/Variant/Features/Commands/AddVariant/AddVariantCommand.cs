namespace Application.Variant.Features.Commands.AddVariant;

public record AddVariantCommand : IRequest<ServiceResult<ProductVariantViewDto>>
{
    public int ProductId { get; init; }
    public string? Sku { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal SellingPrice { get; init; }
    public decimal OriginalPrice { get; init; }
    public int Stock { get; init; }
    public bool IsUnlimited { get; init; }
    public decimal ShippingMultiplier { get; init; } = 1;
    public List<int> AttributeValueIds { get; init; } = new();
    public List<int>? EnabledShippingMethodIds { get; init; }
}