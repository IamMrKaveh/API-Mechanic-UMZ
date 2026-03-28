using Domain.Variant.ValueObjects;

namespace Domain.Variant.Exceptions;

public sealed class InvalidVariantOperationException(
    ProductVariantId variantId,
    string operation,
    string reason)
    : DomainException($"عملیات '{operation}' روی واریانت {variantId} امکان‌پذیر نیست. دلیل: {reason}")
{
    public ProductVariantId VariantId { get; } = variantId;
    public string Operation { get; } = operation;
    public string Reason { get; } = reason;
}