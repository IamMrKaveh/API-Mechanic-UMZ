namespace Domain.Payment.Exceptions;

public sealed class PaymentTransactionNotFoundException : DomainException
{
    public string? Authority { get; }

    public override string ErrorCode => "PAYMENT_TRANSACTION_NOT_FOUND";

    public PaymentTransactionNotFoundException()
        : base("تراکنش پیدا نشد.")
    {
    }

    public PaymentTransactionNotFoundException(string authority)
        : base($"تراکنش با شناسه پرداخت '{authority}' یافت نشد.")
    {
        Authority = authority;
    }
}