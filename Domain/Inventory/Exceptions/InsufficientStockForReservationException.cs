namespace Domain.Inventory.Exceptions;

public class InsufficientStockForReservationException(int variantId, int availableStock, int requestedQuantity) : DomainException($"موجودی کافی برای رزرو نیست. واریانت: {variantId}، موجودی: {availableStock}، درخواستی: {requestedQuantity}")
{
    public int VariantId { get; } = variantId;
    public int AvailableStock { get; } = availableStock;
    public int RequestedQuantity { get; } = requestedQuantity;
    public int Shortage { get; } = requestedQuantity - availableStock;
}