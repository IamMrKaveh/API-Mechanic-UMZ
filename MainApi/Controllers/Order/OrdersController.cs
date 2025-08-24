namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly MechanicContext _context;

    public OrdersController(MechanicContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetTOrders(
        [FromQuery] int? userId = null,
        [FromQuery] int? statusId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _context.TOrders
            .Include(o => o.User)
            .Include(o => o.OrderStatus)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);

        if (statusId.HasValue)
            query = query.Where(o => o.OrderStatusId == statusId.Value);

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate.Value);

        var totalCount = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
                o.Name,
                o.Address,
                o.PostalCode,
                o.TotalAmount,
                o.TotalProfit,
                o.CreatedAt,
                o.DeliveryDate,
                User = new
                {
                    o.User.Id,
                    o.User.PhoneNumber,
                    o.User.FirstName,
                    o.User.LastName
                },
                OrderStatus = new
                {
                    o.OrderStatus.Id,
                    o.OrderStatus.Name,
                    o.OrderStatus.Icon
                },
                OrderItemsCount = o.OrderItems.Count()
            })
            .ToListAsync();

        return Ok(new
        {
            Data = orders,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetOrderById(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid order ID");

        var order = await _context.TOrders
            .Include(o => o.User)
            .Include(o => o.OrderStatus)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.ProductType)
            .Select(o => new
            {
                o.Id,
                o.Name,
                o.Address,
                o.PostalCode,
                o.TotalAmount,
                o.TotalProfit,
                o.CreatedAt,
                o.DeliveryDate,
                User = new
                {
                    o.User.Id,
                    o.User.PhoneNumber,
                    o.User.FirstName,
                    o.User.LastName
                },
                OrderStatus = new
                {
                    o.OrderStatus.Id,
                    o.OrderStatus.Name,
                    o.OrderStatus.Icon
                },
                OrderItems = o.OrderItems.Select(oi => new
                {
                    oi.Id,
                    oi.PurchasePrice,
                    oi.SellingPrice,
                    oi.Quantity,
                    oi.Amount,
                    oi.Profit,
                    Product = new
                    {
                        oi.Product.Id,
                        oi.Product.Name,
                        oi.Product.Icon,
                        ProductType = new
                        {
                            oi.Product.ProductType.Id,
                            oi.Product.ProductType.Name
                        }
                    }
                })
            })
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<TOrders>> PostTOrders(CreateOrderDto orderDto)
    {
        if (orderDto == null)
            return BadRequest("Order data is required");

        if (string.IsNullOrWhiteSpace(orderDto.Address) || string.IsNullOrWhiteSpace(orderDto.PostalCode))
            return BadRequest("Address and PostalCode are required");

        if (orderDto.OrderItems == null || !orderDto.OrderItems.Any())
            return BadRequest("At least one order item is required");

        if (!await _context.TUsers.AnyAsync(u => u.Id == orderDto.UserId))
            return BadRequest("Invalid user ID");

        if (!await _context.TOrderStatus.AnyAsync(s => s.Id == orderDto.OrderStatusId))
            return BadRequest("Invalid order status ID");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = new TOrders
            {
                UserId = orderDto.UserId,
                Name = orderDto.Name,
                Address = orderDto.Address,
                PostalCode = orderDto.PostalCode,
                CreatedAt = DateTime.UtcNow,
                OrderStatusId = orderDto.OrderStatusId,
                DeliveryDate = orderDto.DeliveryDate
            };

            _context.TOrders.Add(order);
            await _context.SaveChangesAsync();

            var totalAmount = 0;
            var totalProfit = 0;

            foreach (var itemDto in orderDto.OrderItems)
            {
                if (itemDto.Quantity <= 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Invalid quantity for product {itemDto.ProductId}");
                }

                if (itemDto.SellingPrice <= 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Invalid selling price for product {itemDto.ProductId}");
                }

                var product = await _context.TProducts.FindAsync(itemDto.ProductId);
                if (product == null)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Product {itemDto.ProductId} not found");
                }

                if ((product.Count ?? 0) < itemDto.Quantity)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Product {itemDto.ProductId} insufficient stock. Available: {product.Count ?? 0}, Requested: {itemDto.Quantity}");
                }

                var orderItem = new TOrderItems
                {
                    UserOrderId = order.Id,
                    ProductId = itemDto.ProductId,
                    PurchasePrice = product.PurchasePrice ?? 0,
                    SellingPrice = itemDto.SellingPrice,
                    Quantity = itemDto.Quantity,
                    Amount = itemDto.SellingPrice * itemDto.Quantity,
                    Profit = (itemDto.SellingPrice - (product.PurchasePrice ?? 0)) * itemDto.Quantity
                };

                totalAmount += orderItem.Amount;
                totalProfit += orderItem.Profit;

                product.Count -= itemDto.Quantity;
                _context.TOrderItems.Add(orderItem);
            }

            order.TotalAmount = totalAmount;
            order.TotalProfit = totalProfit;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPost("checkout-from-cart")]
    public async Task<ActionResult<TOrders>> CheckoutFromCart([FromBody] CreateOrderDto orderDto)
    {
        if (orderDto == null || string.IsNullOrWhiteSpace(orderDto.Address) || string.IsNullOrWhiteSpace(orderDto.PostalCode))
            return BadRequest("Address and PostalCode are required");

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existingOrder = await _context.TOrders
                .FirstOrDefaultAsync(o => o.Name == idempotencyKey && o.UserId == userId.Value);
            if (existingOrder != null)
                return Conflict(new { Message = "Duplicate request", OrderId = existingOrder.Id });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var cart = await _context.TCarts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
                return BadRequest("Cart is empty");

            var order = new TOrders
            {
                UserId = userId.Value,
                Name = idempotencyKey ?? $"Order-{Guid.NewGuid()}",
                Address = orderDto.Address,
                PostalCode = orderDto.PostalCode,
                CreatedAt = DateTime.UtcNow,
                OrderStatusId = 1
            };

            _context.TOrders.Add(order);
            await _context.SaveChangesAsync();

            var totalAmount = 0;
            var totalProfit = 0;

            foreach (var item in cart.CartItems)
            {
                if (item.Quantity <= 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Invalid quantity for product {item.ProductId}");
                }

                var product = await _context.TProducts.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (product == null)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Product {item.ProductId} not found");
                }

                if ((product.Count ?? 0) < item.Quantity)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Product {item.ProductId} insufficient stock. Available: {product.Count ?? 0}, Requested: {item.Quantity}");
                }

                if ((product.SellingPrice ?? 0) <= 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Product {item.ProductId} has invalid price");
                }

                var orderItem = new TOrderItems
                {
                    UserOrderId = order.Id,
                    ProductId = product.Id,
                    PurchasePrice = product.PurchasePrice ?? 0,
                    SellingPrice = product.SellingPrice ?? 0,
                    Quantity = item.Quantity,
                    Amount = (product.SellingPrice ?? 0) * item.Quantity,
                    Profit = ((product.SellingPrice ?? 0) - (product.PurchasePrice ?? 0)) * item.Quantity
                };

                totalAmount += orderItem.Amount;
                totalProfit += orderItem.Profit;

                product.Count -= item.Quantity;
                _context.TOrderItems.Add(orderItem);
            }

            order.TotalAmount = totalAmount;
            order.TotalProfit = totalProfit;

            _context.TCartItems.RemoveRange(cart.CartItems);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTOrders(int id, UpdateOrderDto orderDto)
    {
        if (id <= 0)
            return BadRequest("Invalid order ID");

        if (orderDto == null)
            return BadRequest("Order data is required");

        var order = await _context.TOrders.FindAsync(id);
        if (order == null)
            return NotFound();

        if (orderDto.OrderStatusId.HasValue && !await _context.TOrderStatus.AnyAsync(s => s.Id == orderDto.OrderStatusId.Value))
            return BadRequest("Invalid order status ID");

        order.Name = orderDto.Name ?? order.Name;
        order.Address = orderDto.Address ?? order.Address;
        order.PostalCode = orderDto.PostalCode ?? order.PostalCode;
        order.DeliveryDate = orderDto.DeliveryDate ?? order.DeliveryDate;

        if (orderDto.OrderStatusId.HasValue)
            order.OrderStatusId = orderDto.OrderStatusId.Value;

        try
        {
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TOrdersExists(id))
                return NotFound();
            throw;
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto statusDto)
    {
        if (id <= 0)
            return BadRequest("Invalid order ID");

        if (statusDto == null)
            return BadRequest("Status data is required");

        var order = await _context.TOrders.FindAsync(id);
        if (order == null)
            return NotFound();

        if (!await _context.TOrderStatus.AnyAsync(s => s.Id == statusDto.OrderStatusId))
            return BadRequest("Invalid order status ID");

        order.OrderStatusId = statusDto.OrderStatusId;

        try
        {
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TOrdersExists(id))
                return NotFound();
            throw;
        }
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetOrderStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = _context.TOrders.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate.Value);

        var statistics = await query
            .GroupBy(o => 1)
            .Select(g => new
            {
                TotalOrders = g.Count(),
                TotalRevenue = g.Sum(o => o.TotalAmount),
                TotalProfit = g.Sum(o => o.TotalProfit),
                AverageOrderValue = g.Average(o => (double)o.TotalAmount)
            })
            .FirstOrDefaultAsync();

        var statusStatistics = await _context.TOrders
            .Include(o => o.OrderStatus)
            .Where(o => fromDate == null || o.CreatedAt >= fromDate.Value)
            .Where(o => toDate == null || o.CreatedAt <= toDate.Value)
            .GroupBy(o => new { o.OrderStatusId, o.OrderStatus.Name })
            .Select(g => new
            {
                StatusId = g.Key.OrderStatusId,
                StatusName = g.Key.Name,
                Count = g.Count()
            })
            .ToListAsync();

        return Ok(new
        {
            GeneralStatistics = statistics ?? new
            {
                TotalOrders = 0,
                TotalRevenue = 0,
                TotalProfit = 0,
                AverageOrderValue = 0.0
            },
            StatusStatistics = statusStatistics
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTOrders(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid order ID");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.TOrders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            foreach (var orderItem in order.OrderItems)
            {
                if (orderItem.Product != null)
                {
                    orderItem.Product.Count += orderItem.Quantity;
                }
            }

            _context.TOrderItems.RemoveRange(order.OrderItems);
            _context.TOrders.Remove(order);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private bool TOrdersExists(int id)
    {
        return _context.TOrders.Any(e => e.Id == id);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}