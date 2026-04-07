using Domain.Common.Exceptions;
using Domain.Variant.ValueObjects;

namespace Domain.Product.Exceptions;

public sealed class InsufficientStockException : DomainException
{
    public VariantId VariantId { get; }
    public int AvailableStock { get; }
    public int RequestedQuantity { get; }

    public override string ErrorCode => "INSUFFICIENT_STOCK";

    public InsufficientStockException(VariantId variantId, int availableStock, int requestedQuantity)
        : base($"موجودی کافی نیست. واریانت: {variantId}، موجودی: {availableStock}، درخواستی: {requestedQuantity}")
    {
        VariantId = variantId;
        AvailableStock = availableStock;
        RequestedQuantity = requestedQuantity;
    }
}