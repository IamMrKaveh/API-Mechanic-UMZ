namespace Application.Product.Features.Commands.BulkUpdatePrices;

public record BulkUpdatePricesCommand(ICollection<VariantPriceUpdateInput> Updates) : IRequest<ServiceResult>;

public sealed record VariantPriceUpdateInput(
    Guid ProductId,
    Guid VariantId,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice);