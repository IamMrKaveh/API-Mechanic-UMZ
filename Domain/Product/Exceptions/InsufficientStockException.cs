namespace Domain.Product.Exceptions;

public class InsufficientStockException : DomainException
{
    public int VariantId { get; }
    public int AvailableStock { get; }
    public int RequestedQuantity { get; }

    public InsufficientStockException(int variantId, int availableStock, int requestedQuantity)
        : base($"موجودی کافی نیست. واریانت: {variantId}، موجودی: {availableStock}، درخواستی: {requestedQuantity}")
    {
        VariantId = variantId;
        AvailableStock = availableStock;
        RequestedQuantity = requestedQuantity;
    }
}