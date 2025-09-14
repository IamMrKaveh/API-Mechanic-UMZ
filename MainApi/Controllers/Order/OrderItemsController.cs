namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderItemsController : BaseApiController
{
    private readonly MechanicContext _context;
    private readonly ILogger<OrderItemsController> _logger;

    public OrderItemsController(
        MechanicContext context,
        ILogger<OrderItemsController> logger)
    {
        _context = context;
        _logger = logger;

    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetOrderItems([FromQuery] int? orderId = null)
    {
        var currentUserId = GetCurrentUserId();

        if (!orderId.HasValue)
        {
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }
        }
        else
        {
            if (orderId.Value <= 0)
                return BadRequest("Invalid order ID");

            var order = await _context.TOrders.FindAsync(orderId.Value);
            if (order == null) return NotFound("Order not found");

            if (order.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }
        }

        var query = _context.TOrderItems
            .Include(oi => oi.Product)
                .ThenInclude(p => p.Category)
            .Include(oi => oi.UserOrder)
            .AsQueryable();

        if (orderId.HasValue)
        {
            query = query.Where(oi => oi.UserOrderId == orderId.Value);
        }
        else if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var orderItems = await query
            .Select(oi => new
            {
                oi.Id,
                oi.SellingPrice,
                oi.Quantity,
                Amount = oi.SellingPrice * oi.Quantity,
                Product = new
                {
                    oi.Product.Id,
                    oi.Product.Name,
                    Icon = string.IsNullOrEmpty(oi.Product.Icon) ? null : BaseUrl + oi.Product.Icon,
                    CategoryName = oi.Product.Category != null ? oi.Product.Category.Name : null
                },
                Order = new
                {
                    oi.UserOrder.Id,
                    oi.UserOrder.Name,
                    oi.UserOrder.CreatedAt
                }
            })
            .OrderByDescending(oi => oi.Id)
            .ToListAsync();

        return Ok(orderItems);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetOrderItem(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid order item ID");

        var currentUserId = GetCurrentUserId();

        var orderItemQuery = _context.TOrderItems
            .Where(oi => oi.Id == id);

        if (!User.IsInRole("Admin"))
        {
            orderItemQuery = orderItemQuery.Where(oi => oi.UserOrder.UserId == currentUserId);
        }

        var result = await orderItemQuery.Select(oi => new
        {
            oi.Id,
            oi.PurchasePrice,
            oi.SellingPrice,
            oi.Quantity,
            Amount = oi.SellingPrice * oi.Quantity,
            Profit = (oi.SellingPrice - oi.PurchasePrice) * oi.Quantity,
            Product = new
            {
                oi.Product.Id,
                oi.Product.Name,
                Icon = string.IsNullOrEmpty(oi.Product.Icon) ? null : BaseUrl + oi.Product.Icon,
                CategoryName = oi.Product.Category != null ? oi.Product.Category.Name : null
            },
            Order = new
            {
                oi.UserOrder.Id,
                oi.UserOrder.Name,
                oi.UserOrder.CreatedAt
            }
        }).FirstOrDefaultAsync();


        if (result == null)
            return NotFound("Order item not found");

        if (User.IsInRole("Admin"))
        {
            return Ok(result);
        }

        var publicResult = new
        {
            result.Id,
            result.SellingPrice,
            result.Quantity,
            result.Amount,
            result.Product,
            result.Order
        };

        return Ok(publicResult);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TOrderItems>> CreateOrderItem(CreateOrderItemDto itemDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var order = await _context.TOrders.FindAsync(itemDto.UserOrderId);
        if (order == null)
            return BadRequest("Order not found");

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var product = await _context.TProducts.FindAsync(itemDto.ProductId);
            if (product == null)
                return BadRequest("Product not found");

            if (!product.IsUnlimited && product.Count < itemDto.Quantity)
                return BadRequest($"Not enough stock. Available: {product.Count}, Requested: {itemDto.Quantity}");

            var orderItem = new TOrderItems
            {
                UserOrderId = itemDto.UserOrderId,
                ProductId = itemDto.ProductId,
                PurchasePrice = product.PurchasePrice,
                SellingPrice = itemDto.SellingPrice,
                Quantity = itemDto.Quantity
            };
            _context.TOrderItems.Add(orderItem);

            if (!product.IsUnlimited)
            {
                product.Count -= itemDto.Quantity;
            }

            await _context.SaveChangesAsync();

            await RecalculateOrderTotalsAsync(order.Id);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return CreatedAtAction("GetOrderItem", new { id = orderItem.Id }, orderItem);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict("The item's stock has changed. Please try again.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Error creating order item");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderItem(int id, UpdateOrderItemDto itemDto)
    {
        if (id <= 0)
            return BadRequest("Invalid order item ID");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var orderItem = await _context.TOrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.UserOrder)
                .FirstOrDefaultAsync(oi => oi.Id == id);

            if (orderItem == null)
                return NotFound("Order item not found");

            var originalQuantity = orderItem.Quantity;

            if (itemDto.RowVersion != null)
            {
                _context.Entry(orderItem).OriginalValues["RowVersion"] = itemDto.RowVersion;
            }

            if (itemDto.Quantity.HasValue)
            {
                if (itemDto.Quantity.Value <= 0) return BadRequest("Quantity must be greater than zero");
                var quantityDifference = itemDto.Quantity.Value - originalQuantity;
                if (!orderItem.Product!.IsUnlimited)
                {
                    if (orderItem.Product.Count < quantityDifference)
                        return BadRequest($"Not enough stock. Available: {orderItem.Product.Count}, Additional required: {quantityDifference}");
                    orderItem.Product.Count -= quantityDifference;
                }
                orderItem.Quantity = itemDto.Quantity.Value;
            }

            if (itemDto.SellingPrice.HasValue)
            {
                if (itemDto.SellingPrice.Value <= 0) return BadRequest("Selling price must be greater than zero");
                orderItem.SellingPrice = itemDto.SellingPrice.Value;
            }

            await _context.SaveChangesAsync();
            await RecalculateOrderTotalsAsync(orderItem.UserOrderId);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict("Data has been modified by another user. Please refresh and try again.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Error updating order item");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteOrderItem(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid order item ID");

        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var orderItem = await _context.TOrderItems
                .Include(oi => oi.Product)
                .FirstOrDefaultAsync(oi => oi.Id == id);

            if (orderItem == null)
                return NotFound("Order item not found");

            var orderId = orderItem.UserOrderId;

            if (orderItem.Product != null && !orderItem.Product.IsUnlimited)
            {
                orderItem.Product.Count += orderItem.Quantity;
            }

            _context.TOrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();

            await RecalculateOrderTotalsAsync(orderId);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict("The item's stock or order has changed. Please try again.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Error deleting order item");
        }
    }

    [NonAction]
    private async Task RecalculateOrderTotalsAsync(int orderId)
    {
        var order = await _context.TOrders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order != null)
        {
            order.TotalAmount = order.OrderItems.Sum(oi => oi.SellingPrice * oi.Quantity);
            order.TotalProfit = order.OrderItems.Sum(oi => (oi.SellingPrice - oi.PurchasePrice) * oi.Quantity);
            _context.TOrders.Update(order);
        }
    }
}