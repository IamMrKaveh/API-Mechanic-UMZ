namespace Domain.Payment.Exceptions;

public sealed class PaymentAlreadyVerifiedException(Guid transactionId, long refId, DateTime? verifiedAt = null) : DomainException($"تراکنش {transactionId} قبلاً با کد پیگیری {refId} تأیید شده است.")
{
    public Guid TransactionId { get; } = transactionId;
    public long RefId { get; } = refId;
    public DateTime? VerifiedAt { get; } = verifiedAt;
}