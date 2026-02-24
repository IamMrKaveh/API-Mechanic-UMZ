namespace Application.Product.Features.Commands.BulkUpdatePrices;

public record BulkUpdatePricesCommand : IRequest<ServiceResult>
{
    public List<VariantPriceUpdateInput> Updates { get; init; } = new();
}

public record VariantPriceUpdateInput
{
    public int ProductId { get; init; }
    public int VariantId { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal SellingPrice { get; init; }
    public decimal OriginalPrice { get; init; }
}