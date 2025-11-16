namespace Application.Common.Interfaces.Persistence;

public interface IOrderRepository
{
    Task<(IEnumerable<Domain.Order.Order> Orders, int TotalItems)> GetOrdersAsync(int? currentUserId, bool isAdmin, int? userId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);

    Task<Domain.Order.Order?> GetOrderByIdAsync(int orderId, int? currentUserId, bool isAdmin);

    Task<Domain.Order.Order?> GetOrderByIdempotencyKey(string idempotencyKey, int userId);

    Task<Domain.Order.Order?> GetOrderForPaymentAsync(int orderId);

    Task<Domain.Order.Order?> GetOrderForUpdateAsync(int orderId);

    Task<Domain.Order.Order?> GetOrderWithItemsAsync(int orderId);

    Task<Dictionary<int, Domain.Product.ProductVariant>> GetVariantsByIdsAsync(List<int> variantIds);

    Task<Domain.Order.ShippingMethod?> GetShippingMethodAsync(int shippingMethodId);

    Task<Domain.Payment.PaymentTransaction?> GetPaymentTransactionAsync(string authority);

    Task AddOrderAsync(Domain.Order.Order order);

    Task AddDiscountUsageAsync(Domain.Discount.DiscountUsage discountUsage);

    Task AddPaymentTransactionAsync(Domain.Payment.PaymentTransaction transaction);

    void SetOrderRowVersion(Domain.Order.Order order, byte[] rowVersion);

    void DeleteOrder(Domain.Order.Order order);

    Task<bool> OrderStatusExistsAsync(int statusId);

    Task<string?> GetOrderStatusNameAsync(int statusId);

    Task<object> GetOrderStatisticsAsync(DateTime? fromDate, DateTime? toDate);
}