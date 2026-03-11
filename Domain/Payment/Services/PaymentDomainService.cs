namespace Domain.Payment.Services;

public sealed class PaymentDomainService
{
    public static PaymentProcessResult ProcessSuccessfulPayment(
        PaymentTransaction transaction,
        long refId,
        decimal fee = 0)
    {
        Guard.Against.Null(transaction, nameof(transaction));

        if (!transaction.CanBeVerified())
            return PaymentProcessResult.Failed("تراکنش قابل تأیید نیست.");

        transaction.MarkAsSuccess(refId, fee);

        return PaymentProcessResult.Success(refId);
    }

    public static void ProcessFailedPayment(
        PaymentTransaction transaction,
        string? errorMessage = null)
    {
        Guard.Against.Null(transaction, nameof(transaction));

        transaction.MarkAsFailed(errorMessage);
    }

    public static int ExpireStaleTransactions(IEnumerable<PaymentTransaction> transactions)
    {
        Guard.Against.Null(transactions, nameof(transactions));

        var expiredCount = 0;

        foreach (var transaction in transactions)
        {
            if (transaction.IsExpired() && transaction.IsPending())
            {
                transaction.Expire();
                expiredCount++;
            }
        }

        return expiredCount;
    }
}