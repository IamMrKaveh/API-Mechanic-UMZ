namespace Application.Order.Contracts;

public interface IOrderQueryService
{
    Task<OrderDto?> GetOrderDetailsAsync(int orderId, int userId, CancellationToken ct = default);

    Task<AdminOrderDto?> GetAdminOrderDetailsAsync(int orderId, CancellationToken ct = default);

    Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(int userId, string? status, int page, int pageSize, CancellationToken ct = default);

    Task<PaginatedResult<AdminOrderDto>> GetAdminOrdersAsync(int? userId, string? status, DateTime? fromDate, DateTime? toDate, bool? isPaid, int page, int pageSize, CancellationToken ct = default);

    Task<OrderStatisticsDto> GetOrderStatisticsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);

    Task<bool> HasUserPurchasedProductAsync(int userId, int productId, CancellationToken ct = default);

    Task<int> CountByUserIdAsync(int userId, CancellationToken ct = default);

    Task<decimal> GetTotalSpentByUserIdAsync(int userId, CancellationToken ct = default);

    Task<OrderDto?> GetOrderByIdAsync(int orderId, CancellationToken ct = default);

    Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(int userId, int page, int pageSize, CancellationToken ct = default);
}