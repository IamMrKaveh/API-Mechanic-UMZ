namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrdersController : BaseApiController
{
    private readonly MechanicContext _context;
    private readonly ILogger<OrdersController> _logger;
    private readonly IRateLimitService _rateLimitService;

    public OrdersController(
        MechanicContext context,
        ILogger<OrdersController> logger,
        IRateLimitService rateLimitService)
    {
        _context = context;
        _logger = logger;
        _rateLimitService = rateLimitService;
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

        var isAdmin = User.IsInRole("Admin");
        var currentUserId = GetCurrentUserId();

        if (!userId.HasValue && !isAdmin)
        {
            userId = currentUserId;
        }
        else if (userId.HasValue && userId.Value != currentUserId && !isAdmin)
        {
            return Forbid();
        }

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

        var totalItems = await query.CountAsync();
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
                TotalProfit = isAdmin ? (int?)o.TotalProfit : null,
                o.CreatedAt,
                o.DeliveryDate,
                User = new
                {
                    o.User!.Id,
                    o.User.PhoneNumber,
                    o.User.FirstName,
                    o.User.LastName
                },
                OrderStatus = new
                {
                    o.OrderStatus!.Id,
                    o.OrderStatus.Name,
                    o.OrderStatus.Icon
                },
                OrderItemsCount = o.OrderItems.Count
            })
            .ToListAsync();

        return Ok(new
        {
            Items = orders,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetOrderById(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid order ID");

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        var query = _context.TOrders
            .Include(o => o.User)
            .Include(o => o.OrderStatus)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p!.Category)
            .Where(o => o.Id == id);

        if (!User.IsInRole("Admin"))
        {
            query = query.Where(o => o.UserId == currentUserId);
        }

        var order = await query.Select(o => new
        {
            o.Id,
            o.UserId,
            o.Name,
            o.Address,
            o.PostalCode,
            o.TotalAmount,
            TotalProfit = User.IsInRole("Admin") ? (int?)o.TotalProfit : null,
            o.CreatedAt,
            o.DeliveryDate,
            o.OrderStatusId,
            User = new
            {
                o.User!.Id,
                o.User.PhoneNumber,
                o.User.FirstName,
                o.User.LastName,
                o.User.IsAdmin
            },
            OrderStatus = new
            {
                o.OrderStatus!.Id,
                o.OrderStatus.Name,
                o.OrderStatus.Icon
            },
            OrderItems = o.OrderItems.Select(oi => new
            {
                oi.Id,
                oi.ProductId,
                PurchasePrice = User.IsInRole("Admin") ? (int?)oi.PurchasePrice : null,
                oi.SellingPrice,
                oi.Quantity,
                Amount = oi.Amount,
                Profit = User.IsInRole("Admin") ? (int?)oi.Profit : null,
                Product = new
                {
                    oi.Product!.Id,
                    oi.Product.Name,
                    oi.Product.Icon,
                    Category = oi.Product.Category != null ? new
                    {
                        oi.Product.Category.Id,
                        oi.Product.Category.Name
                    } : null
                }
            })
        })
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return NotFound("Order not found");
        }

        return Ok(order);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TOrders>> PostTOrders(CreateOrderDto orderDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(orderDto.Address) || string.IsNullOrWhiteSpace(orderDto.PostalCode))
            return BadRequest("Address and PostalCode are required");

        if (orderDto.OrderItems == null || !orderDto.OrderItems.Any())
            return BadRequest("At least one order item is required");

        var userExists = await _context.TUsers.AnyAsync(u => u.Id == orderDto.UserId);
        if (!userExists)
            return BadRequest("Invalid user ID");

        var statusExists = await _context.TOrderStatus.AnyAsync(s => s.Id == orderDto.OrderStatusId);
        if (!statusExists)
            return BadRequest("Invalid order status ID");

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(idempotencyKey))
            return BadRequest("Idempotency-Key header is required.");

        var existingOrder = await _context.TOrders.FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey);
        if (existingOrder != null)
            return CreatedAtAction(nameof(GetOrderById), new { id = existingOrder.Id }, existingOrder);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var productIds = orderDto.OrderItems.Select(i => i.ProductId).ToList();
            var products = await _context.TProducts
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            foreach (var itemDto in orderDto.OrderItems)
            {
                if (!products.TryGetValue(itemDto.ProductId, out var product))
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Product {itemDto.ProductId} not found");
                }

                if (itemDto.SellingPrice < product.PurchasePrice)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Selling price for product {product.Name} cannot be less than its purchase price.");
                }

                if (!product.IsUnlimited && product.Count < itemDto.Quantity)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Product {itemDto.ProductId} insufficient stock. Available: {product.Count}, Requested: {itemDto.Quantity}");
                }

                if (!product.IsUnlimited)
                {
                    product.Count = product.Count - itemDto.Quantity;
                }
            }

            var sanitizer = new HtmlSanitizer();
            var order = new TOrders
            {
                UserId = orderDto.UserId,
                Name = sanitizer.Sanitize(orderDto.Name),
                Address = sanitizer.Sanitize(orderDto.Address),
                PostalCode = sanitizer.Sanitize(orderDto.PostalCode),
                CreatedAt = DateTime.UtcNow,
                OrderStatusId = orderDto.OrderStatusId,
                DeliveryDate = orderDto.DeliveryDate,
                IdempotencyKey = idempotencyKey
            };

            _context.TOrders.Add(order);
            await _context.SaveChangesAsync();

            var orderItems = orderDto.OrderItems.Select(itemDto =>
            {
                var product = products[itemDto.ProductId];
                return new TOrderItems
                {
                    UserOrderId = order.Id,
                    ProductId = itemDto.ProductId,
                    PurchasePrice = product.PurchasePrice,
                    SellingPrice = itemDto.SellingPrice,
                    Quantity = itemDto.Quantity,
                };
            }).ToList();

            _context.TOrderItems.AddRange(orderItems);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            await transaction.RollbackAsync();
            var duplicateOrder = await _context.TOrders.FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey);
            if (duplicateOrder != null)
                return CreatedAtAction(nameof(GetOrderById), new { id = duplicateOrder.Id }, duplicateOrder);
            return Conflict("Duplicate request. Order already exists.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, $"An error occurred while creating the order: {ex.Message}");
        }
    }

    [HttpPost("checkout-from-cart")]
    public async Task<ActionResult<object>> CheckoutFromCart([FromBody] CreateOrderFromCartDto orderDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized("User not authenticated");

        var rateLimitKey = $"checkout_{userId.Value}";
        if (await _rateLimitService.IsLimitedAsync(rateLimitKey, 3, 1))
        {
            _logger.LogWarning("Rate limit exceeded for checkout by user {UserId}", userId.Value);
            return StatusCode(429, "Too many checkout attempts. Please try again in a minute.");
        }

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(idempotencyKey))
            return BadRequest("Idempotency-Key header is required.");

        var existingOrderByIdempotency = await _context.TOrders.FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey && o.UserId == userId.Value);
        if (existingOrderByIdempotency != null)
            return CreatedAtAction(nameof(GetOrderById), new { id = existingOrderByIdempotency.Id }, await GetOrderDtoById(existingOrderByIdempotency.Id));

        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var cart = await _context.TCarts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null || !cart.CartItems.Any())
                return BadRequest("Cart is empty");

            const int pendingPaymentStatusId = 1;

            var sanitizer = new HtmlSanitizer();
            var order = new TOrders
            {
                UserId = userId.Value,
                Name = sanitizer.Sanitize(orderDto.Name),
                Address = sanitizer.Sanitize(orderDto.Address),
                PostalCode = sanitizer.Sanitize(orderDto.PostalCode),
                CreatedAt = DateTime.UtcNow,
                OrderStatusId = pendingPaymentStatusId,
                IdempotencyKey = idempotencyKey,
            };

            foreach (var item in cart.CartItems)
            {
                var product = item.Product;
                if (product == null)
                {
                    await transaction.RollbackAsync();
                    return Conflict($"Product with ID '{item.ProductId}' not found.");
                }

                if (!product.IsUnlimited)
                {
                    var productInDb = await _context.TProducts.FindAsync(product.Id);
                    if (productInDb!.Count < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return Conflict($"Product '{product.Name}' is out of stock or has insufficient quantity.");
                    }
                    productInDb.Count -= item.Quantity;
                }

                order.OrderItems.Add(new TOrderItems
                {
                    ProductId = item.ProductId,
                    PurchasePrice = product.PurchasePrice,
                    SellingPrice = product.SellingPrice,
                    Quantity = item.Quantity,
                });
            }

            _context.TOrders.Add(order);
            _context.TCartItems.RemoveRange(cart.CartItems);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var createdOrderDto = await GetOrderDtoById(order.Id);
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, createdOrderDto);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            await transaction.RollbackAsync();
            var existingOrder = await _context.TOrders.FirstOrDefaultAsync(o => o.UserId == userId.Value && o.IdempotencyKey == idempotencyKey);
            if (existingOrder != null)
                return CreatedAtAction(nameof(GetOrderById), new { id = existingOrder.Id }, await GetOrderDtoById(existingOrder.Id));

            return Conflict("A concurrency error occurred. Please try again.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict("The stock for an item in your cart has changed. Please review your cart and try again.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "An error occurred while processing the checkout.");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutTOrders(int id, UpdateOrderDto orderDto)
    {
        if (id <= 0)
            return BadRequest("Invalid order ID");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var order = await _context.TOrders.FindAsync(id);
        if (order == null)
            return NotFound("Order not found");

        if (orderDto.RowVersion != null)
        {
            _context.Entry(order).Property("RowVersion").OriginalValue = orderDto.RowVersion;
        }
        else
        {
            return BadRequest("RowVersion is required for concurrency control.");
        }

        if (orderDto.OrderStatusId.HasValue)
        {
            var statusExists = await _context.TOrderStatus.AnyAsync(s => s.Id == orderDto.OrderStatusId.Value);
            if (!statusExists)
                return BadRequest("Invalid order status ID");
            order.OrderStatusId = orderDto.OrderStatusId.Value;
        }

        var sanitizer = new HtmlSanitizer();
        if (!string.IsNullOrWhiteSpace(orderDto.Name))
            order.Name = sanitizer.Sanitize(orderDto.Name);
        if (!string.IsNullOrWhiteSpace(orderDto.Address))
            order.Address = sanitizer.Sanitize(orderDto.Address);
        if (!string.IsNullOrWhiteSpace(orderDto.PostalCode))
            order.PostalCode = sanitizer.Sanitize(orderDto.PostalCode);
        if (orderDto.DeliveryDate.HasValue)
            order.DeliveryDate = orderDto.DeliveryDate;

        try
        {
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "The order was modified by another user. Please reload and try again." });
        }
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto statusDto)
    {
        if (id <= 0)
            return BadRequest("Invalid order ID");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var order = await _context.TOrders.FindAsync(id);
        if (order == null)
            return NotFound("Order not found");

        var statusExists = await _context.TOrderStatus.AnyAsync(s => s.Id == statusDto.OrderStatusId);
        if (!statusExists)
            return BadRequest("Invalid order status ID");

        order.OrderStatusId = statusDto.OrderStatusId;

        try
        {
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await TOrdersExistsAsync(id))
                return NotFound("Order not found");
            throw;
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while updating the order status");
        }
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> GetOrderStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
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
                .Where(o => !fromDate.HasValue || o.CreatedAt >= fromDate.Value)
                .Where(o => !toDate.HasValue || o.CreatedAt <= toDate.Value)
                .GroupBy(o => new { o.OrderStatusId, o.OrderStatus!.Name })
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
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while retrieving statistics");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
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
                return NotFound("Order not found");

            const int shippedStatusId = 3;
            const int deliveredStatusId = 4;

            if (order.OrderStatusId >= shippedStatusId)
            {
                return BadRequest("Cannot delete an order that has been shipped or delivered. Consider changing its status instead.");
            }

            foreach (var orderItem in order.OrderItems)
            {
                if (orderItem.Product != null && !orderItem.Product.IsUnlimited)
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
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "An error occurred while deleting the order with ID {OrderId}", id);
            return StatusCode(500, "An error occurred while deleting the order");
        }
    }

    [NonAction]
    private async Task<bool> TOrdersExistsAsync(int id)
    {
        return await _context.TOrders.AnyAsync(e => e.Id == id);
    }

    [NonAction]
    private async Task<object?> GetOrderDtoById(int id)
    {
        var isAdmin = User.IsInRole("Admin");
        return await _context.TOrders
            .Where(o => o.Id == id)
            .Select(o => new
            {
                o.Id,
                o.UserId,
                o.Name,
                o.Address,
                o.PostalCode,
                o.TotalAmount,
                TotalProfit = isAdmin ? (int?)o.TotalProfit : null,
                o.CreatedAt,
                o.DeliveryDate,
                o.OrderStatusId,
                OrderStatus = new { o.OrderStatus.Id, o.OrderStatus.Name, o.OrderStatus.Icon },
                User = new { o.User.Id, o.User.PhoneNumber, o.User.FirstName, o.User.LastName, o.User.IsAdmin },
                OrderItems = o.OrderItems.Select(oi => new
                {
                    oi.Id,
                    oi.ProductId,
                    PurchasePrice = isAdmin ? (int?)oi.PurchasePrice : null,
                    oi.SellingPrice,
                    oi.Quantity,
                    Amount = oi.Amount,
                    Profit = isAdmin ? (int?)oi.Profit : null,
                    Product = new { oi.Product.Id, oi.Product.Name, oi.Product.Icon }
                })
            })
            .FirstOrDefaultAsync();
    }
}