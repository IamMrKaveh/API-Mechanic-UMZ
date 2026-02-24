namespace Domain.Payment.ValueObjects;

public sealed class PaymentStatistics : ValueObject
{
    public int TotalTransactions { get; }
    public int SuccessfulTransactions { get; }
    public int FailedTransactions { get; }
    public int PendingTransactions { get; }
    public int ExpiredTransactions { get; }
    public int RefundedTransactions { get; }
    public Money TotalSuccessfulAmount { get; }
    public Money TotalRefundedAmount { get; }
    public Money TotalFees { get; }

    private PaymentStatistics(
        int totalTransactions,
        int successfulTransactions,
        int failedTransactions,
        int pendingTransactions,
        int expiredTransactions,
        int refundedTransactions,
        Money totalSuccessfulAmount,
        Money totalRefundedAmount,
        Money totalFees)
    {
        TotalTransactions = totalTransactions;
        SuccessfulTransactions = successfulTransactions;
        FailedTransactions = failedTransactions;
        PendingTransactions = pendingTransactions;
        ExpiredTransactions = expiredTransactions;
        RefundedTransactions = refundedTransactions;
        TotalSuccessfulAmount = totalSuccessfulAmount;
        TotalRefundedAmount = totalRefundedAmount;
        TotalFees = totalFees;
    }

    public static PaymentStatistics Create(
        int totalTransactions,
        int successfulTransactions,
        int failedTransactions,
        int pendingTransactions,
        int expiredTransactions,
        int refundedTransactions,
        decimal totalSuccessfulAmount,
        decimal totalRefundedAmount,
        decimal totalFees)
    {
        return new PaymentStatistics(
            totalTransactions,
            successfulTransactions,
            failedTransactions,
            pendingTransactions,
            expiredTransactions,
            refundedTransactions,
            Money.FromDecimal(totalSuccessfulAmount),
            Money.FromDecimal(totalRefundedAmount),
            Money.FromDecimal(totalFees));
    }

    public static PaymentStatistics Empty() =>
        Create(0, 0, 0, 0, 0, 0, 0, 0, 0);

    
    public decimal SuccessRate =>
        TotalTransactions > 0
            ? Math.Round((decimal)SuccessfulTransactions / TotalTransactions * 100, 2)
            : 0;

    public decimal FailureRate =>
        TotalTransactions > 0
            ? Math.Round((decimal)FailedTransactions / TotalTransactions * 100, 2)
            : 0;

    public decimal ExpiredRate =>
        TotalTransactions > 0
            ? Math.Round((decimal)ExpiredTransactions / TotalTransactions * 100, 2)
            : 0;

    public Money NetSuccessfulAmount =>
        TotalSuccessfulAmount.Subtract(TotalRefundedAmount);

    public Money NetAmountAfterFees =>
        TotalSuccessfulAmount.Subtract(TotalFees);

    public decimal AverageTransactionAmount =>
        SuccessfulTransactions > 0
            ? Math.Round(TotalSuccessfulAmount.Amount / SuccessfulTransactions, 0)
            : 0;

    public bool HasPendingTransactions => PendingTransactions > 0;

    public bool IsHealthy => SuccessRate >= 90 && ExpiredRate < 5;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return TotalTransactions;
        yield return SuccessfulTransactions;
        yield return TotalSuccessfulAmount;
    }
}