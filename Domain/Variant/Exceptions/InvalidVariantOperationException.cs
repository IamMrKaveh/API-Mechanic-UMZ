namespace Domain.Variant.Exceptions;

public sealed class InvalidVariantOperationException : DomainException
{
    public int VariantId { get; }
    public string Operation { get; }
    public string Reason { get; }

    public InvalidVariantOperationException(int variantId, string operation, string reason)
        : base($"عملیات '{operation}' روی واریانت {variantId} امکان‌پذیر نیست. دلیل: {reason}")
    {
        VariantId = variantId;
        Operation = operation;
        Reason = reason;
    }
}