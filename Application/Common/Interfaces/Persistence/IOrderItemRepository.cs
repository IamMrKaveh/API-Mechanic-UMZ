namespace Application.Common.Interfaces.Persistence;

public interface IOrderItemRepository
{
    Task<(IEnumerable<Domain.Order.OrderItem> items, int total)> GetOrderItemsAsync(int? currentUserId, bool isAdmin, int? orderId, int page, int pageSize);

    Task<Domain.Order.OrderItem?> GetOrderItemByIdAsync(int orderItemId);

    Task<Domain.Order.OrderItem?> GetOrderItemWithDetailsAsync(int orderItemId);

    Task<Domain.Product.ProductVariant?> GetProductVariantWithProductAsync(int variantId);

    Task AddOrderItemAsync(Domain.Order.OrderItem orderItem);

    void SetOrderItemRowVersion(Domain.Order.OrderItem item, byte[] rowVersion);

    void DeleteOrderItem(Domain.Order.OrderItem item);
}