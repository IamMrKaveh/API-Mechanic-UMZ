namespace Infrastructure.Persistence.Interface.Order;

public interface IOrderRepository
{
    Task<(IEnumerable<Domain.Order.Order> Orders, int TotalItems)> GetOrdersAsync(int? userId, bool isAdmin, int? filterUserId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<Domain.Order.Order?> GetOrderByIdAsync(int orderId, int? userId, bool isAdmin);
    Task<Domain.Order.Order?> GetOrderByAuthorityAsync(string authority);
    Task<Domain.Order.Order?> GetOrderForUpdateAsync(int orderId);
    Task<Domain.Order.Order?> GetOrderForPaymentAsync(int orderId);
    Task<Domain.Order.Order?> GetOrderWithItemsAsync(int orderId);
    Task<Domain.Order.Order?> GetOrderByIdempotencyKey(string idempotencyKey, int userId);
    Task<bool> GetExistingPendingOrder(int userId);
    Task<IEnumerable<ProductVariant>> GetVariantsByIdsAsync(IEnumerable<int> variantIds);
    Task<IEnumerable<ProductVariant>> GetVariantsByIdsForUpdateAsync(IEnumerable<int> variantIds);
    Task<ShippingMethod?> GetShippingMethodByIdAsync(int shippingMethodId);
    Task<ShippingMethod?> GetShippingMethodAsync(int shippingMethodId);
    Task AddAsync(Domain.Order.Order order);
    Task AddOrderAsync(Domain.Order.Order order);
    Task AddDiscountUsageAsync(DiscountUsage discountUsage);
    Task AddPaymentTransactionAsync(PaymentTransaction paymentTransaction);
    Task<PaymentTransaction?> GetPaymentTransactionAsync(string authority);
    Task<PaymentTransaction?> GetPaymentTransactionForUpdateAsync(string authority);
    void Update(Domain.Order.Order order);
    void SetOriginalRowVersion(Domain.Order.Order order, byte[] rowVersion);
    Task<object> GetOrderStatisticsAsync(DateTime? fromDate, DateTime? toDate);
    Task<IEnumerable<object>> GetOrderStatusStatisticsAsync(DateTime? fromDate, DateTime? toDate);
    Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey);
    Task<List<ShippingMethod>> GetShippingMethodsByIdsAsync(List<int> shippingMethodIds);
    Task<List<ProductVariant>> GetVariantsWithShippingMethodsAsync(List<int> variantIds);
}