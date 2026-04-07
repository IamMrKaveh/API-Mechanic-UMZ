using Domain.Common.Exceptions;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Exceptions;

public sealed class InvalidVariantOperationException : DomainException
{
    public VariantId VariantId { get; }
    public string Operation { get; }
    public string Reason { get; }

    public override string ErrorCode => "INVALID_VARIANT_OPERATION";

    public InvalidVariantOperationException(VariantId variantId, string operation, string reason)
        : base($"عملیات '{operation}' روی واریانت {variantId} امکان‌پذیر نیست. دلیل: {reason}")
    {
        VariantId = variantId;
        Operation = operation;
        Reason = reason;
    }
}