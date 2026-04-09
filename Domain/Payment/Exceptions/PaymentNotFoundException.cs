namespace Domain.Payment.Exceptions;

public sealed class PaymentNotFoundException : DomainException
{
    public string? Authority { get; }
    public Guid? TransactionId { get; }

    public override string ErrorCode => "PAYMENT_NOT_FOUND";

    public PaymentNotFoundException(string authority)
        : base($"تراکنش پرداخت با شناسه '{authority}' یافت نشد.")
    {
        Authority = authority;
    }

    public PaymentNotFoundException(Guid transactionId)
        : base($"تراکنش پرداخت با شناسه {transactionId} یافت نشد.")
    {
        TransactionId = transactionId;
    }
}