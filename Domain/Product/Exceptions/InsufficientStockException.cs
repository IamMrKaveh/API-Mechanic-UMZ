namespace Domain.Product.Exceptions;

public class InsufficientStockException(int variantId, int availableStock, int requestedQuantity) : DomainException($"موجودی کافی نیست. واریانت: {variantId}، موجودی: {availableStock}، درخواستی: {requestedQuantity}")
{
    public int VariantId { get; } = variantId;
    public int AvailableStock { get; } = availableStock;
    public int RequestedQuantity { get; } = requestedQuantity;
}