using Domain.Payment.Aggregates;
using Domain.Payment.Results;

namespace Domain.Payment.Services;

public sealed class PaymentDomainService()
{
    public static PaymentProcessResult ProcessSuccessfulPayment(
        PaymentTransaction transaction,
        long refId,
        decimal fee = 0)
    {
        Guard.Against.Null(transaction, nameof(transaction));

        var now = DateTime.UtcNow;

        if (!transaction.CanBeVerified(now))
            return PaymentProcessResult.Failed("تراکنش قابل تأیید نیست.");

        transaction.MarkAsSuccess(refId, now, fee);

        return PaymentProcessResult.Success(refId);
    }

    public static void ProcessFailedPayment(
        PaymentTransaction transaction,
        string? errorMessage = null)
    {
        Guard.Against.Null(transaction, nameof(transaction));

        transaction.MarkAsFailed(DateTime.UtcNow, errorMessage);
    }

    public static int ExpireStaleTransactions(IEnumerable<PaymentTransaction> transactions)
    {
        Guard.Against.Null(transactions, nameof(transactions));

        var now = DateTime.UtcNow;
        var expiredCount = 0;

        foreach (var transaction in transactions)
        {
            if (transaction.IsExpired(now) && transaction.IsPending())
            {
                transaction.Expire(now);
                expiredCount++;
            }
        }

        return expiredCount;
    }
}