using Domain.Payment.Aggregates;

namespace Domain.Payment.Services;

public sealed class PaymentDomainService()
{
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