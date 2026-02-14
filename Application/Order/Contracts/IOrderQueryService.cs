namespace Application.Order.Contracts;

public interface IOrderQueryService
{
    /// <summary>
    /// دریافت جزئیات سفارش برای کاربر
    /// </summary>
    Task<OrderDto?> GetOrderDetailsAsync(int orderId, int userId, CancellationToken ct = default);

    /// <summary>
    /// دریافت جزئیات سفارش برای مدیر (شامل اطلاعات بیشتر)
    /// </summary>
    Task<AdminOrderDto?> GetAdminOrderDetailsAsync(int orderId, CancellationToken ct = default);

    /// <summary>
    /// دریافت سفارشات کاربر با صفحه‌بندی
    /// </summary>
    Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(
        int userId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت سفارشات برای مدیر با فیلتر
    /// </summary>
    Task<PaginatedResult<AdminOrderDto>> GetAdminOrdersAsync(
        int? userId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        bool? isPaid,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت آمار سفارشات
    /// </summary>
    Task<OrderStatisticsDto> GetOrderStatisticsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct = default);

    /// <summary>
    /// بررسی خرید محصول توسط کاربر
    /// </summary>
    Task<bool> HasUserPurchasedProductAsync(
        int userId,
        int productId,
        CancellationToken ct = default);

    /// <summary>
    /// شمارش سفارشات کاربر
    /// </summary>
    Task<int> CountByUserIdAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// مجموع خرید کاربر
    /// </summary>
    Task<decimal> GetTotalSpentByUserIdAsync(int userId, CancellationToken ct = default);

    Task<OrderDto?> GetOrderByIdAsync(int orderId, CancellationToken ct = default);

    Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(int userId, int page, int pageSize, CancellationToken ct = default);

    Task<PaginatedResult<OrderDto>> GetAdminOrdersAsync(
        string? status, string? searchTerm, DateTime? fromDate, DateTime? toDate,
        int page, int pageSize, CancellationToken ct = default);

    Task<OrderStatistics> GetOrderStatisticsAsync(CancellationToken ct = default);
}