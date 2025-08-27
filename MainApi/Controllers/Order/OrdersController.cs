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
                    .ThenInclude(p => p!.ProductType)
            .Where(o => o.Id == id)
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
                        oi.Product!.Id,
                        oi.Product.Name,
                        oi.Product.Icon,
                        ProductType = oi.Product.ProductType != null ? new
                        {
                            oi.Product.ProductType.Id,
                            oi.Product.ProductType.Name
                        } : null
                    }
                })
            })
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound("Order not found");

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

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var productIds = orderDto.OrderItems.Select(i => i.ProductId).ToList();
            var products = await _context.TProducts
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            var totalAmount = 0;
            var totalProfit = 0;

            foreach (var itemDto in orderDto.OrderItems)
            {
                if (!products.TryGetValue(itemDto.ProductId, out var product))
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Product {itemDto.ProductId} not found");
                }

                if ((product.Count ?? 0) < itemDto.Quantity)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Product {itemDto.ProductId} insufficient stock. Available: {product.Count ?? 0}, Requested: {itemDto.Quantity}");
                }

                product.Count = Math.Max(0, (product.Count ?? 0) - itemDto.Quantity);

                var amount = itemDto.SellingPrice * itemDto.Quantity;
                var profit = (itemDto.SellingPrice - (product.PurchasePrice ?? 0)) * itemDto.Quantity;

                totalAmount += amount;
                totalProfit += profit;
            }

            var order = new TOrders
            {
                UserId = orderDto.UserId,
                Name = orderDto.Name,
                Address = orderDto.Address,
                PostalCode = orderDto.PostalCode,
                CreatedAt = DateTime.UtcNow,
                OrderStatusId = orderDto.OrderStatusId,
                DeliveryDate = orderDto.DeliveryDate,
                TotalAmount = totalAmount,
                TotalProfit = totalProfit
            };

            _context.TOrders.Add(order);
            await _context.SaveChangesAsync();

            var orderItems = orderDto.OrderItems.Select(itemDto => new TOrderItems
            {
                UserOrderId = order.Id,
                ProductId = itemDto.ProductId,
                PurchasePrice = products[itemDto.ProductId].PurchasePrice ?? 0,
                SellingPrice = itemDto.SellingPrice,
                Quantity = itemDto.Quantity,
                Amount = itemDto.SellingPrice * itemDto.Quantity,
                Profit = (itemDto.SellingPrice - (products[itemDto.ProductId].PurchasePrice ?? 0)) * itemDto.Quantity
            }).ToList();

            _context.TOrderItems.AddRange(orderItems);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, $"An error occurred while creating the order: {ex.Message}");
        }
    }

    [HttpPost("checkout-from-cart")]
    public async Task<ActionResult<TOrders>> CheckoutFromCart([FromBody] CreateOrderDto orderDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(orderDto.Address) || string.IsNullOrWhiteSpace(orderDto.PostalCode))
            return BadRequest("Address and PostalCode are required");

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized("User not authenticated");

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

            if (cart == null || !cart.CartItems.Any())
            {
                await transaction.RollbackAsync();
                return BadRequest("Cart is empty");
            }

            var productUpdates = new List<TProducts>();
            var totalAmount = 0;
            var totalProfit = 0;
            var orderItems = new List<TOrderItems>();

            foreach (var item in cart.CartItems)
            {
                if (item.Quantity <= 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Invalid quantity for product {item.ProductId}");
                }

                var product = await _context.TProducts
                    .Where(p => p.Id == item.ProductId)
                    .FirstOrDefaultAsync();

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

                product.Count = Math.Max(0, (product.Count ?? 0) - item.Quantity);
                productUpdates.Add(product);

                var orderItem = new TOrderItems
                {
                    ProductId = product.Id,
                    PurchasePrice = product.PurchasePrice ?? 0,
                    SellingPrice = product.SellingPrice ?? 0,
                    Quantity = item.Quantity,
                    Amount = (product.SellingPrice ?? 0) * item.Quantity,
                    Profit = ((product.SellingPrice ?? 0) - (product.PurchasePrice ?? 0)) * item.Quantity
                };

                totalAmount += orderItem.Amount;
                totalProfit += orderItem.Profit;
                orderItems.Add(orderItem);
            }

            var order = new TOrders
            {
                UserId = userId.Value,
                Name = idempotencyKey ?? $"Order-{Guid.NewGuid()}",
                Address = orderDto.Address,
                PostalCode = orderDto.PostalCode,
                CreatedAt = DateTime.UtcNow,
                OrderStatusId = 1,
                TotalAmount = totalAmount,
                TotalProfit = totalProfit
            };

            _context.TOrders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var orderItem in orderItems)
            {
                orderItem.UserOrderId = order.Id;
            }

            _context.TOrderItems.AddRange(orderItems);
            _context.TProducts.UpdateRange(productUpdates);
            _context.TCartItems.RemoveRange(cart.CartItems);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "An error occurred while processing the checkout");
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

        if (orderDto.OrderStatusId.HasValue)
        {
            var statusExists = await _context.TOrderStatus.AnyAsync(s => s.Id == orderDto.OrderStatusId.Value);
            if (!statusExists)
                return BadRequest("Invalid order status ID");
        }

        if (!string.IsNullOrWhiteSpace(orderDto.Name))
            order.Name = orderDto.Name;
        if (!string.IsNullOrWhiteSpace(orderDto.Address))
            order.Address = orderDto.Address;
        if (!string.IsNullOrWhiteSpace(orderDto.PostalCode))
            order.PostalCode = orderDto.PostalCode;
        if (orderDto.DeliveryDate.HasValue)
            order.DeliveryDate = orderDto.DeliveryDate;
        if (orderDto.OrderStatusId.HasValue)
            order.OrderStatusId = orderDto.OrderStatusId.Value;

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
            return StatusCode(500, "An error occurred while updating the order");
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

            foreach (var orderItem in order.OrderItems)
            {
                if (orderItem.Product != null)
                {
                    orderItem.Product.Count = (orderItem.Product.Count ?? 0) + orderItem.Quantity;
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
            return StatusCode(500, "An error occurred while deleting the order");
        }
    }

    [NonAction]
    private async Task<bool> TOrdersExistsAsync(int id)
    {
        return await _context.TOrders.AnyAsync(e => e.Id == id);
    }

    [NonAction]
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}