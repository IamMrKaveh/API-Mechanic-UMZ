namespace Domain.Inventory.Exceptions;

public class InsufficientStockForReservationException : DomainException
{
    public int VariantId { get; }
    public int AvailableStock { get; }
    public int RequestedQuantity { get; }
    public int Shortage { get; }

    public InsufficientStockForReservationException(int variantId, int availableStock, int requestedQuantity)
        : base($"موجودی کافی برای رزرو نیست. واریانت: {variantId}، موجودی: {availableStock}، درخواستی: {requestedQuantity}")
    {
        VariantId = variantId;
        AvailableStock = availableStock;
        RequestedQuantity = requestedQuantity;
        Shortage = requestedQuantity - availableStock;
    }
}