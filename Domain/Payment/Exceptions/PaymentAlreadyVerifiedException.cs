namespace Domain.Payment.Exceptions;

public sealed class PaymentAlreadyVerifiedException : DomainException
{
    public int TransactionId { get; }
    public long RefId { get; }
    public DateTime? VerifiedAt { get; }

    public PaymentAlreadyVerifiedException(int transactionId, long refId, DateTime? verifiedAt = null)
        : base($"تراکنش {transactionId} قبلاً با کد پیگیری {refId} تأیید شده است.")
    {
        TransactionId = transactionId;
        RefId = refId;
        VerifiedAt = verifiedAt;
    }
}