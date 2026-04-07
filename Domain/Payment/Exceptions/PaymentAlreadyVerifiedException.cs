using Domain.Common.Exceptions;

namespace Domain.Payment.Exceptions;

public sealed class PaymentAlreadyVerifiedException : DomainException
{
    public Guid TransactionId { get; }
    public long RefId { get; }
    public DateTime? VerifiedAt { get; }

    public override string ErrorCode => "PAYMENT_ALREADY_VERIFIED";

    public PaymentAlreadyVerifiedException(Guid transactionId, long refId, DateTime? verifiedAt = null)
        : base($"تراکنش {transactionId} قبلاً با کد پیگیری {refId} تأیید شده است.")
    {
        TransactionId = transactionId;
        RefId = refId;
        VerifiedAt = verifiedAt;
    }
}