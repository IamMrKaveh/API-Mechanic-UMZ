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
    public async Task<ActionResult<IEnumerable<object>>> GetTOrderItems([FromQuery] int? orderId = null)
    {
        var query = _context.TOrderItems
            .Include(oi => oi.Product)
                .ThenInclude(p => p.ProductType)
            .Include(oi => oi.UserOrder)
            .AsQueryable();

        if (orderId.HasValue)
            query = query.Where(oi => oi.UserOrderId == orderId.Value);

        var orderItems = await query
            .Select(oi => new
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
                },
                Order = new
                {
                    oi.UserOrder.Id,
                    oi.UserOrder.Name,
                    oi.UserOrder.CreatedAt
                }
            })
            .ToListAsync();

        return Ok(orderItems);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetTOrderItems(int id)
    {
        var orderItem = await _context.TOrderItems
            .Include(oi => oi.Product)
                .ThenInclude(p => p.ProductType)
            .Include(oi => oi.UserOrder)
                .ThenInclude(o => o.User)
            .Select(oi => new
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
                    oi.Product.PurchasePrice,
                    oi.Product.SellingPrice,
                    oi.Product.Count,
                    ProductType = new
                    {
                        oi.Product.ProductType.Id,
                        oi.Product.ProductType.Name
                    }
                },
                Order = new
                {
                    oi.UserOrder.Id,
                    oi.UserOrder.Name,
                    oi.UserOrder.Address,
                    oi.UserOrder.CreatedAt,
                    User = new
                    {
                        oi.UserOrder.User.Id,
                        oi.UserOrder.User.PhoneNumber,
                        oi.UserOrder.User.FirstName,
                        oi.UserOrder.User.LastName
                    }
                }
            })
            .FirstOrDefaultAsync(oi => oi.Id == id);

        if (orderItem == null)
            return NotFound();

        return Ok(orderItem);
    }

    [HttpPost]
    public async Task<ActionResult<TOrderItems>> PostTOrderItems(CreateOrderItemDto itemDto)
    {
        var product = await _context.TProducts.FindAsync(itemDto.ProductId);
        if (product == null)
            return BadRequest("Product not found");

        if ((product.Count ?? 0) < itemDto.Quantity)
            return BadRequest("Insufficient product stock");

        var order = await _context.TOrders.FindAsync(itemDto.UserOrderId);
        if (order == null)
            return BadRequest("Order not found");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var orderItem = new TOrderItems
            {
                UserOrderId = itemDto.UserOrderId,
                ProductId = itemDto.ProductId,
                PurchasePrice = product.PurchasePrice ?? 0,
                SellingPrice = itemDto.SellingPrice,
                Quantity = itemDto.Quantity,
                Amount = itemDto.SellingPrice * itemDto.Quantity,
                Profit = (itemDto.SellingPrice - (product.PurchasePrice ?? 0)) * itemDto.Quantity
            };

            _context.TOrderItems.Add(orderItem);
            product.Count -= itemDto.Quantity;

            order.TotalAmount += orderItem.Amount;
            order.TotalProfit += orderItem.Profit;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction("GetTOrderItems", new { id = orderItem.Id }, orderItem);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTOrderItems(int id, UpdateOrderItemDto itemDto)
    {
        var orderItem = await _context.TOrderItems
            .Include(oi => oi.Product)
            .Include(oi => oi.UserOrder)
            .FirstOrDefaultAsync(oi => oi.Id == id);

        if (orderItem == null)
            return NotFound();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var oldAmount = orderItem.Amount;
            var oldProfit = orderItem.Profit;
            var oldQuantity = orderItem.Quantity;

            if (itemDto.Quantity.HasValue && itemDto.Quantity.Value != oldQuantity)
            {
                var quantityDifference = itemDto.Quantity.Value - oldQuantity;

                if ((orderItem.Product.Count ?? 0) < quantityDifference)
                    return BadRequest("Insufficient product stock");

                orderItem.Product.Count -= quantityDifference;
                orderItem.Quantity = itemDto.Quantity.Value;
            }

            if (itemDto.SellingPrice.HasValue)
                orderItem.SellingPrice = itemDto.SellingPrice.Value;

            orderItem.Amount = orderItem.SellingPrice * orderItem.Quantity;
            orderItem.Profit = (orderItem.SellingPrice - orderItem.PurchasePrice) * orderItem.Quantity;

            orderItem.UserOrder.TotalAmount = orderItem.UserOrder.TotalAmount - oldAmount + orderItem.Amount;
            orderItem.UserOrder.TotalProfit = orderItem.UserOrder.TotalProfit - oldProfit + orderItem.Profit;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            if (!TOrderItemsExists(id))
                return NotFound();
            throw;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTOrderItems(int id)
    {
        var orderItem = await _context.TOrderItems
            .Include(oi => oi.Product)
            .Include(oi => oi.UserOrder)
            .FirstOrDefaultAsync(oi => oi.Id == id);

        if (orderItem == null)
            return NotFound();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            orderItem.Product.Count += orderItem.Quantity;
            orderItem.UserOrder.TotalAmount -= orderItem.Amount;
            orderItem.UserOrder.TotalProfit -= orderItem.Profit;

            _context.TOrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private bool TOrderItemsExists(int id)
    {
        return _context.TOrderItems.Any(e => e.Id == id);
    }
}