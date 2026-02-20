namespace Application.Order.Features.Commands.ExpireOrders;

/// <summary>
/// دستور انقضای خودکار سفارش‌های پرداخت‌نشده.
/// توسط BackgroundService به صورت دوره‌ای اجرا می‌شود.
/// </summary>
public sealed record ExpireOrdersCommand : IRequest<ExpireOrdersResult>;

public sealed record ExpireOrdersResult(int ExpiredCount, IEnumerable<int> ExpiredOrderIds);