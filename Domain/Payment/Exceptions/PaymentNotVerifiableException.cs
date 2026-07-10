using Domain.Payment.ValueObjects;

namespace Domain.Payment.Exceptions;

public sealed class PaymentNotVerifiableException : DomainException
{
    public PaymentAuthority? Authority { get; }

    public override string ErrorCode => "PAYMENT_NOT_VERIFIABLE";

    public PaymentNotVerifiableException()
        : base("تراکنش قابل تأیید نیست.")
    {
    }

    public PaymentNotVerifiableException(PaymentAuthority authority)
        : base($"تراکنش پرداخت با شناسه '{authority}' در وضعیت قابل تأیید قرار ندارد.")
    {
        Authority = authority;
    }
}