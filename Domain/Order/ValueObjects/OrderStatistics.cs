namespace Domain.Order.ValueObjects;

public sealed class OrderStatistics : ValueObject
{
    public int TotalOrders { get; }
    public int PaidOrders { get; }
    public int PendingOrders { get; }
    public int CancelledOrders { get; }
    public int ProcessingOrders { get; }
    public int ShippedOrders { get; }
    public int DeliveredOrders { get; }
    public Money TotalRevenue { get; }
    public Money TotalProfit { get; }
    public Money AverageOrderValue { get; }
    public Dictionary<string, int> StatusBreakdown { get; }

    private OrderStatistics(
        int totalOrders,
        int paidOrders,
        int pendingOrders,
        int cancelledOrders,
        int processingOrders,
        int shippedOrders,
        int deliveredOrders,
        Money totalRevenue,
        Money totalProfit,
        Dictionary<string, int> statusBreakdown)
    {
        TotalOrders = totalOrders;
        PaidOrders = paidOrders;
        PendingOrders = pendingOrders;
        CancelledOrders = cancelledOrders;
        ProcessingOrders = processingOrders;
        ShippedOrders = shippedOrders;
        DeliveredOrders = deliveredOrders;
        TotalRevenue = totalRevenue;
        TotalProfit = totalProfit;
        StatusBreakdown = statusBreakdown;

        AverageOrderValue = paidOrders > 0
            ? Money.FromDecimal(totalRevenue.Amount / paidOrders)
            : Money.Zero();
    }

    public static OrderStatistics Create(
        int totalOrders,
        int paidOrders,
        int pendingOrders,
        int cancelledOrders,
        int processingOrders,
        int shippedOrders,
        int deliveredOrders,
        decimal totalRevenue,
        decimal totalProfit,
        Dictionary<string, int>? statusBreakdown = null)
    {
        return new OrderStatistics(
            totalOrders,
            paidOrders,
            pendingOrders,
            cancelledOrders,
            processingOrders,
            shippedOrders,
            deliveredOrders,
            Money.FromDecimal(totalRevenue),
            Money.FromDecimal(totalProfit),
            statusBreakdown ?? new Dictionary<string, int>());
    }

    public static OrderStatistics Empty() => Create(0, 0, 0, 0, 0, 0, 0, 0, 0);

    public decimal GetPaidOrdersPercentage() =>
        TotalOrders > 0 ? Math.Round((decimal)PaidOrders / TotalOrders * 100, 2) : 0;

    public decimal GetCancellationRate() =>
        TotalOrders > 0 ? Math.Round((decimal)CancelledOrders / TotalOrders * 100, 2) : 0;

    public decimal GetProfitMargin() =>
        TotalRevenue.Amount > 0
            ? Math.Round(TotalProfit.Amount / TotalRevenue.Amount * 100, 2)
            : 0;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return TotalOrders;
        yield return PaidOrders;
        yield return TotalRevenue;
        yield return TotalProfit;
    }
}