namespace Domain.Variant.Exceptions;

public sealed class VariantNotFoundException : DomainException
{
    public int VariantId { get; }
    public int? ProductId { get; }

    public VariantNotFoundException(int variantId, int? productId = null)
        : base($"واریانت با شناسه {variantId} یافت نشد.")
    {
        VariantId = variantId;
        ProductId = productId;
    }
}