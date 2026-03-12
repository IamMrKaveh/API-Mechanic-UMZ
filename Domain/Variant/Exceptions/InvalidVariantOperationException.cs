namespace Domain.Variant.Exceptions;

public sealed class InvalidVariantOperationException(int variantId, string operation, string reason)
    : DomainException($"عملیات '{operation}' روی واریانت {variantId} امکان‌پذیر نیست. دلیل: {reason}")
{
    public int VariantId { get; } = variantId;
    public string Operation { get; } = operation;
    public string Reason { get; } = reason;
}