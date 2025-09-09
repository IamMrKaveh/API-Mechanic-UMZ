namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderItemsController : ControllerBase
{
    private readonly MechanicContext _context;

    public OrderItemsController(MechanicContext context)
    {
        _context = context;
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
            .Select(oi => new PublicOrderItemViewDto
            {
                Id = oi.Id,
                SellingPrice = oi.SellingPrice,
                Quantity = oi.Quantity,
                Amount = oi.Amount,
                Product = new PublicOrderItemProductViewDto
                {
                    Id = oi.Product.Id,
                    Name = oi.Product.Name,
                    Icon = oi.Product.Icon,
                    CategoryName = oi.Product.Category != null ? oi.Product.Category.Name : null
                },
                Order = new PublicOrderItemOrderViewDto
                {
                    Id = oi.UserOrder.Id,
                    Name = oi.UserOrder.Name,
                    CreatedAt = oi.UserOrder.CreatedAt
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

        var orderItem = await _context.TOrderItems
            .Include(oi => oi.Product)
                .ThenInclude(p => p.Category)
            .Include(oi => oi.UserOrder)
                .ThenInclude(o => o.User)
            .Where(oi => oi.Id == id)
            .Select(oi => new
            {
                OrderItem = oi,
                OrderUserId = oi.UserOrder.UserId
            })
            .FirstOrDefaultAsync();

        if (orderItem == null)
            return NotFound("Order item not found");

        if (orderItem.OrderUserId != currentUserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var result = new
        {
            orderItem.OrderItem.Id,
            orderItem.OrderItem.PurchasePrice,
            orderItem.OrderItem.SellingPrice,
            orderItem.OrderItem.Quantity,
            orderItem.OrderItem.Amount,
            orderItem.OrderItem.Profit,
            Product = new
            {
                orderItem.OrderItem.Product.Id,
                orderItem.OrderItem.Product.Name,
                orderItem.OrderItem.Product.Icon,
                orderItem.OrderItem.Product.PurchasePrice,
                orderItem.OrderItem.Product.SellingPrice,
                orderItem.OrderItem.Product.Count,
                Category = orderItem.OrderItem.Product.Category != null ? new
                {
                    orderItem.OrderItem.Product.Category.Id,
                    orderItem.OrderItem.Product.Category.Name
                } : null
            },
            Order = new
            {
                orderItem.OrderItem.UserOrder.Id,
                orderItem.OrderItem.UserOrder.Name,
                orderItem.OrderItem.UserOrder.Address,
                orderItem.OrderItem.UserOrder.CreatedAt,
                User = new
                {
                    orderItem.OrderItem.UserOrder.User.Id,
                    orderItem.OrderItem.UserOrder.User.PhoneNumber,
                    orderItem.OrderItem.UserOrder.User.FirstName,
                    orderItem.OrderItem.UserOrder.User.LastName
                }
            }
        };

        return Ok(result);
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

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var product = await _context.TProducts.FindAsync(itemDto.ProductId);
            if (product == null)
                return BadRequest("Product not found");

            if (product.Count < itemDto.Quantity)
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

            product.Count = product.Count - itemDto.Quantity;
            _context.TProducts.Update(product);

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

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var orderItem = await _context.TOrderItems
                .Include(oi => oi.Product)
                .FirstOrDefaultAsync(oi => oi.Id == id);

            if (orderItem == null)
                return NotFound("Order item not found");

            if (itemDto.Quantity.HasValue)
            {
                if (itemDto.Quantity.Value <= 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest("Quantity must be greater than zero");
                }

                if (itemDto.Quantity.Value != orderItem.Quantity)
                {
                    var quantityDifference = itemDto.Quantity.Value - orderItem.Quantity;
                    var currentStock = orderItem.Product!.Count;

                    if (currentStock < quantityDifference)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"Not enough stock. Available: {currentStock}, Additional required: {quantityDifference}");
                    }

                    orderItem.Product.Count = currentStock - quantityDifference;
                    orderItem.Quantity = itemDto.Quantity.Value;
                    _context.TProducts.Update(orderItem.Product);
                }
            }

            if (itemDto.SellingPrice.HasValue)
            {
                if (itemDto.SellingPrice.Value <= 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest("Selling price must be greater than zero");
                }
                orderItem.SellingPrice = itemDto.SellingPrice.Value;
            }

            _context.TOrderItems.Update(orderItem);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            if (!await OrderItemExistsAsync(id))
                return NotFound("Order item not found");
            return Conflict("Data has been modified by another user.");
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

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var orderItem = await _context.TOrderItems
                .Include(oi => oi.Product)
                .FirstOrDefaultAsync(oi => oi.Id == id);

            if (orderItem == null)
                return NotFound("Order item not found");

            if (orderItem.Product != null)
            {
                orderItem.Product.Count = orderItem.Product.Count + orderItem.Quantity;
                _context.TProducts.Update(orderItem.Product);
            }

            _context.TOrderItems.Remove(orderItem);
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
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [NonAction]
    private async Task<bool> OrderItemExistsAsync(int id)
    {
        return await _context.TOrderItems.AnyAsync(e => e.Id == id);
    }
}