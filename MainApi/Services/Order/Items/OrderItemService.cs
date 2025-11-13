using MainApi.Services.Inventory;
using MainApi.Services.Media;
using System;

namespace MainApi.Services.Order.Items;

public class OrderItemService : IOrderItemService
{
    private readonly MechanicContext _context;
    private readonly ILogger<OrderItemService> _logger;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IMediaService _mediaService;
    private readonly IInventoryService _inventoryService;

    public OrderItemService(MechanicContext context, ILogger<OrderItemService> logger, IHtmlSanitizer htmlSanitizer, IMediaService mediaService, IInventoryService inventoryService)
    {
        _context = context;
        _logger = logger;
        _htmlSanitizer = htmlSanitizer;
        _mediaService = mediaService;
        _inventoryService = inventoryService;
    }

    public async Task<(IEnumerable<object> items, int total)> GetOrderItemsAsync(int? currentUserId, bool isAdmin, int? orderId, int page, int pageSize)
    {
        var query = _context.TOrderItems
            .Include(oi => oi.Variant.Product)
            .Include(oi => oi.Order)
            .AsQueryable();

        if (orderId.HasValue)
        {
            query = query.Where(oi => oi.OrderId == orderId.Value);
        }

        if (!isAdmin)
        {
            query = query.Where(oi => oi.Order != null && oi.Order.UserId == currentUserId);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(oi => oi.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(oi => new
            {
                oi.Id,
                oi.OrderId,
                oi.VariantId,
                ProductName = oi.Variant.Product != null ? oi.Variant.Product.Name : "N/A",
                PurchasePrice = isAdmin ? (decimal?)oi.PurchasePrice : null,
                oi.SellingPrice,
                oi.Quantity,
                oi.Amount,
                Profit = isAdmin ? (decimal?)oi.Profit : null,
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task<object?> GetOrderItemByIdAsync(int orderItemId, int? currentUserId, bool isAdmin)
    {
        var query = _context.TOrderItems
            .Include(oi => oi.Variant.Product.CategoryGroup.Category)
            .Include(oi => oi.Order)
            .Where(oi => oi.Id == orderItemId);

        var item = await query.FirstOrDefaultAsync();

        if (item == null) return null;

        if (!isAdmin && (item.Order == null || item.Order.UserId != currentUserId))
        {
            _logger.LogWarning("Unauthorized access attempt for OrderItem {OrderItemId} by User {UserId}", orderItemId, currentUserId);
            return null;
        }
        var icon = await _mediaService.GetPrimaryImageUrlAsync("Product", item.Variant.ProductId);
        return new
        {
            item.Id,
            item.OrderId,
            item.VariantId,
            Product = item.Variant.Product != null ? new
            {
                item.Variant.Product.Id,
                item.Variant.Product.Name,
                Icon = icon,
                Category = item.Variant.Product.CategoryGroup != null && item.Variant.Product.CategoryGroup.Category != null ? new { item.Variant.Product.CategoryGroup.Category.Id, item.Variant.Product.CategoryGroup.Category.Name } : null
            } : null,
            PurchasePrice = isAdmin ? (decimal?)item.PurchasePrice : null,
            item.SellingPrice,
            item.Quantity,
            item.Amount,
            Profit = isAdmin ? (decimal?)item.Profit : null,
            item.RowVersion
        };
    }

    public async Task<TOrderItems> CreateOrderItemAsync(CreateOrderItemDto itemDto)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        TOrderItems? newOrderItem = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.TOrders.FindAsync(itemDto.OrderId);
                if (order == null) throw new KeyNotFoundException("Order not found");

                var variant = await _context.TProductVariant
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == itemDto.VariantId);

                if (variant?.Product == null) throw new KeyNotFoundException("Product variant not found");
                var product = variant.Product;

                await _inventoryService.LogTransactionAsync(variant.Id, "Sale", -itemDto.Quantity, null, order.UserId, $"Added to order {order.Id}");

                var amount = itemDto.SellingPrice * itemDto.Quantity;
                var profit = (itemDto.SellingPrice - variant.PurchasePrice) * itemDto.Quantity;

                newOrderItem = new TOrderItems
                {
                    OrderId = itemDto.OrderId,
                    VariantId = itemDto.VariantId,
                    Quantity = itemDto.Quantity,
                    SellingPrice = itemDto.SellingPrice,
                    PurchasePrice = variant.PurchasePrice
                };

                _context.TOrderItems.Add(newOrderItem);

                order.TotalAmount += amount;
                order.TotalProfit += profit;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
        return newOrderItem!;
    }

    public async Task<bool> UpdateOrderItemAsync(int orderItemId, UpdateOrderItemDto itemDto, int userId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        var success = false;
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var item = await _context.TOrderItems
                    .Include(oi => oi.Variant.Product)
                    .Include(oi => oi.Order)
                    .FirstOrDefaultAsync(oi => oi.Id == orderItemId);

                if (item?.Variant?.Product == null || item.Order == null)
                    throw new KeyNotFoundException("Order item, variant, product, or order not found.");

                var product = item.Variant.Product;

                if (itemDto.RowVersion != null)
                    _context.Entry(item).Property(p => p.RowVersion).OriginalValue = itemDto.RowVersion;

                var oldAmount = item.Amount;
                var oldProfit = item.Profit;
                var quantityChange = 0;

                if (itemDto.Quantity.HasValue)
                {
                    quantityChange = itemDto.Quantity.Value - item.Quantity;
                    if (quantityChange != 0)
                    {
                        await _inventoryService.LogTransactionAsync(item.VariantId, "OrderItemUpdate", -quantityChange, item.Id, userId, $"Quantity updated in order {item.OrderId}");
                    }
                    item.Quantity = itemDto.Quantity.Value;
                }

                if (itemDto.SellingPrice.HasValue)
                {
                    if (itemDto.SellingPrice.Value < item.Variant.PurchasePrice)
                        throw new ArgumentException("Selling price cannot be less than purchase price.");
                    item.SellingPrice = itemDto.SellingPrice.Value;
                }

                var newAmount = item.SellingPrice * item.Quantity;
                var newProfit = (item.SellingPrice - item.Variant.PurchasePrice) * item.Quantity;

                item.Order.TotalAmount = item.Order.TotalAmount - oldAmount + newAmount;
                item.Order.TotalProfit = item.Order.TotalProfit - oldProfit + newProfit;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                success = true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
        return success;
    }


    public async Task<bool> DeleteOrderItemAsync(int orderItemId, int userId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        var success = false;
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var item = await _context.TOrderItems
                    .Include(oi => oi.Variant.Product)
                    .Include(oi => oi.Order)
                    .FirstOrDefaultAsync(oi => oi.Id == orderItemId);

                if (item?.Order == null) throw new KeyNotFoundException("Order item or order not found.");

                await _inventoryService.LogTransactionAsync(item.VariantId, "Return", item.Quantity, item.Id, userId, $"Item removed from order {item.OrderId}");

                item.Order.TotalAmount -= item.Amount;
                item.Order.TotalProfit -= item.Profit;

                _context.TOrderItems.Remove(item);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                success = true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
        return success;
    }
}