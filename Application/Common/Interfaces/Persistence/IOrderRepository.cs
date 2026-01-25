namespace Application.Common.Interfaces.Persistence;

public interface IOrderRepository
{
    Task<(IEnumerable<Order> Orders, int TotalItems)> GetOrdersAsync(int? userId, bool isAdmin, int? filterUserId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<Order?> GetOrderByIdAsync(int orderId, int? userId, bool isAdmin);
    Task<Order?> GetOrderByAuthorityAsync(string authority);
    Task<Order?> GetOrderForUpdateAsync(int orderId);
    Task<Order?> GetOrderForPaymentAsync(int orderId);
    Task<Order?> GetOrderWithItemsAsync(int orderId);
    Task<Order?> GetOrderByIdempotencyKey(string idempotencyKey, int userId);
    Task<IEnumerable<ProductVariant>> GetVariantsByIdsAsync(IEnumerable<int> variantIds);
    Task<IEnumerable<ProductVariant>> GetVariantsByIdsForUpdateAsync(IEnumerable<int> variantIds);
    Task<ShippingMethod?> GetShippingMethodByIdAsync(int shippingMethodId);
    Task<ShippingMethod?> GetShippingMethodAsync(int shippingMethodId);
    Task AddAsync(Order order);
    Task AddOrderAsync(Order order);
    Task AddDiscountUsageAsync(DiscountUsage discountUsage);
    Task AddPaymentTransactionAsync(PaymentTransaction paymentTransaction);
    Task<PaymentTransaction?> GetPaymentTransactionAsync(string authority);
    Task<PaymentTransaction?> GetPaymentTransactionForUpdateAsync(string authority);
    void Update(Order order);
    void SetOriginalRowVersion(Order order, byte[] rowVersion);
    Task<object> GetOrderStatisticsAsync(DateTime? fromDate, DateTime? toDate);
    Task<IEnumerable<object>> GetOrderStatusStatisticsAsync(DateTime? fromDate, DateTime? toDate);
    Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey);
    Task<List<ShippingMethod>> GetShippingMethodsByIdsAsync(List<int> shippingMethodIds);
    Task<List<ProductVariant>> GetVariantsWithShippingMethodsAsync(List<int> variantIds);
}