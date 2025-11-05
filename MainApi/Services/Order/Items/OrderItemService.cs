namespace MainApi.Services.Order.Items;

public class OrderItemService : IOrderItemService
{
    private readonly MechanicContext _context;
    private readonly ILogger<OrderItemService> _logger;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly string _baseUrl;

    public OrderItemService(MechanicContext context, ILogger<OrderItemService> logger, IHtmlSanitizer htmlSanitizer, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _htmlSanitizer = htmlSanitizer;
        _baseUrl = configuration["LiaraStorage:BaseUrl"] ?? "https://storage.c2.liara.space/mechanic-umz";
    }

    private string? ToAbsoluteUrl(string? relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl))
            return null;
        if (Uri.IsWellFormedUriString(relativeUrl, UriKind.Absolute))
            return relativeUrl;

        var cleanRelative = relativeUrl.TrimStart('~', '/', 'c');
        return $"{_baseUrl}/{cleanRelative}";
    }

    public async Task<(IEnumerable<object> items, int total)> GetOrderItemsAsync(int? currentUserId, bool isAdmin, int? orderId, int page, int pageSize)
    {
        var query = _context.TOrderItems
            .Include(oi => oi.Product)
            .Include(oi => oi.UserOrder)
            .AsQueryable();

        if (orderId.HasValue)
        {
            query = query.Where(oi => oi.UserOrderId == orderId.Value);
        }

        if (!isAdmin)
        {
            query = query.Where(oi => oi.UserOrder != null && oi.UserOrder.UserId == currentUserId);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(oi => oi.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(oi => new
            {
                oi.Id,
                oi.UserOrderId,
                oi.ProductId,
                ProductName = oi.Product != null ? oi.Product.Name : "N/A",
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
            .Include(oi => oi.Product)
            .ThenInclude(p => p!.Category)
            .Include(oi => oi.UserOrder)
            .Where(oi => oi.Id == orderItemId);

        var item = await query.FirstOrDefaultAsync();

        if (item == null) return null;

        if (!isAdmin && (item.UserOrder == null || item.UserOrder.UserId != currentUserId))
        {
            _logger.LogWarning("Unauthorized access attempt for OrderItem {OrderItemId} by User {UserId}", orderItemId, currentUserId);
            return null;
        }

        return new
        {
            item.Id,
            item.UserOrderId,
            item.ProductId,
            Product = item.Product != null ? new
            {
                item.Product.Id,
                item.Product.Name,
                Icon = ToAbsoluteUrl(item.Product.Icon),
                Category = item.Product.Category != null ? new { item.Product.Category.Id, item.Product.Category.Name } : null
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
                var order = await _context.TOrders.FindAsync(itemDto.UserOrderId);
                if (order == null) throw new KeyNotFoundException("Order not found");

                var product = await _context.TProducts.FindAsync(itemDto.ProductId);
                if (product == null) throw new KeyNotFoundException("Product not found");

                if (!product.IsUnlimited)
                {
                    if (product.Count < itemDto.Quantity)
                        throw new InvalidOperationException($"Insufficient stock for product {product.Name}.");
                    product.Count -= itemDto.Quantity;
                }

                var amount = itemDto.SellingPrice * itemDto.Quantity;
                var profit = (itemDto.SellingPrice - product.PurchasePrice) * itemDto.Quantity;

                newOrderItem = new TOrderItems
                {
                    UserOrderId = itemDto.UserOrderId,
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    SellingPrice = itemDto.SellingPrice,
                    PurchasePrice = product.PurchasePrice,
                    Amount = amount,
                    Profit = profit
                };

                _context.TOrderItems.Add(newOrderItem);

                order.TotalAmount += (int)amount;
                order.TotalProfit += (int)profit;

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

    public async Task<bool> UpdateOrderItemAsync(int orderItemId, UpdateOrderItemDto itemDto)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        var success = false;
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var item = await _context.TOrderItems
                    .Include(oi => oi.Product)
                    .Include(oi => oi.UserOrder)
                    .FirstOrDefaultAsync(oi => oi.Id == orderItemId);

                if (item == null || item.Product == null || item.UserOrder == null)
                    throw new KeyNotFoundException("Order item, product, or order not found.");

                if (itemDto.RowVersion != null)
                    _context.Entry(item).Property(p => p.RowVersion).OriginalValue = itemDto.RowVersion;

                var oldAmount = item.Amount;
                var oldProfit = item.Profit;
                var quantityChange = 0;

                if (itemDto.Quantity.HasValue)
                {
                    quantityChange = itemDto.Quantity.Value - item.Quantity;
                    if (!item.Product.IsUnlimited)
                    {
                        if (item.Product.Count < quantityChange)
                            throw new InvalidOperationException("Insufficient stock.");
                        item.Product.Count -= quantityChange;
                    }
                    item.Quantity = itemDto.Quantity.Value;
                }

                if (itemDto.SellingPrice.HasValue)
                {
                    if (itemDto.SellingPrice.Value < item.Product.PurchasePrice)
                        throw new ArgumentException("Selling price cannot be less than purchase price.");
                    item.SellingPrice = itemDto.SellingPrice.Value;
                }

                item.Amount = item.SellingPrice * item.Quantity;
                item.Profit = (item.SellingPrice - item.Product.PurchasePrice) * item.Quantity;

                item.UserOrder.TotalAmount = item.UserOrder.TotalAmount - (int)oldAmount + (int)item.Amount;
                item.UserOrder.TotalProfit = item.UserOrder.TotalProfit - (int)oldProfit + (int)item.Profit;

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


    public async Task<bool> DeleteOrderItemAsync(int orderItemId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        var success = false;
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var item = await _context.TOrderItems
                    .Include(oi => oi.Product)
                    .Include(oi => oi.UserOrder)
                    .FirstOrDefaultAsync(oi => oi.Id == orderItemId);

                if (item == null || item.UserOrder == null) throw new KeyNotFoundException("Order item or order not found.");

                if (item.Product != null && !item.Product.IsUnlimited)
                {
                    item.Product.Count += item.Quantity;
                }

                item.UserOrder.TotalAmount -= (int)item.Amount;
                item.UserOrder.TotalProfit -= (int)item.Profit;

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