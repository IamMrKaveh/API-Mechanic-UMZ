namespace Domain.Payment.Exceptions;

public sealed class PaymentNotFoundException : DomainException
{
    public string? Authority { get; }
    public Guid? TransactionId { get; }

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