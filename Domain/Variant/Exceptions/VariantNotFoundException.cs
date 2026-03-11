namespace Domain.Variant.Exceptions;

public sealed class VariantNotFoundException(int variantId, int? productId = null) : DomainException($"واریانت با شناسه {variantId} یافت نشد.")
{
    public int VariantId { get; } = variantId;
    public int? ProductId { get; } = productId;
}