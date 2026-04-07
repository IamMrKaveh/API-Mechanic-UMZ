using Domain.Common.Exceptions;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Exceptions;

public sealed class InsufficientStockForReservationException : DomainException
{
    public VariantId VariantId { get; }
    public int AvailableStock { get; }
    public int RequestedQuantity { get; }

    public override string ErrorCode => "INSUFFICIENT_STOCK_FOR_RESERVATION";

    public InsufficientStockForReservationException(VariantId variantId, int availableStock, int requestedQuantity)
        : base($"موجودی کافی برای رزرو نیست. واریانت: {variantId}، موجودی: {availableStock}، درخواستی: {requestedQuantity}")
    {
        VariantId = variantId;
        AvailableStock = availableStock;
        RequestedQuantity = requestedQuantity;
    }
}