namespace Domain.Payment.Exceptions;

public sealed class PaymentNotFoundException : DomainException
{
    public string? Authority { get; }
    public int? TransactionId { get; }

    public PaymentNotFoundException(string authority)
        : base($"تراکنش پرداخت با شناسه '{authority}' یافت نشد.")
    {
        Authority = authority;
    }

    public PaymentNotFoundException(int transactionId)
        : base($"تراکنش پرداخت با شناسه عددی {transactionId} یافت نشد.")
    {
        TransactionId = transactionId;
    }
}