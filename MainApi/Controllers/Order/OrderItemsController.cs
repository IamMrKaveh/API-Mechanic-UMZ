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
        var query = _context.TOrderItems
            .Include(oi => oi.Product)
                .ThenInclude(p => p.ProductType)
            .Include(oi => oi.UserOrder)
            .AsQueryable();

        if (orderId.HasValue)
        {
            if (orderId.Value <= 0)
                return BadRequest("شناسه سفارش نامعتبر است");

            query = query.Where(oi => oi.UserOrderId == orderId.Value);
        }

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
                    ProductType = oi.Product.ProductType != null ? new
                    {
                        oi.Product.ProductType.Id,
                        oi.Product.ProductType.Name
                    } : null
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
            return BadRequest("شناسه آیتم سفارش نامعتبر است");

        var orderItem = await _context.TOrderItems
            .Include(oi => oi.Product)
                .ThenInclude(p => p.ProductType)
            .Include(oi => oi.UserOrder)
                .ThenInclude(o => o.User)
            .Where(oi => oi.Id == id)
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
                    ProductType = oi.Product.ProductType != null ? new
                    {
                        oi.Product.ProductType.Id,
                        oi.Product.ProductType.Name
                    } : null
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
            .FirstOrDefaultAsync();

        if (orderItem == null)
            return NotFound("آیتم سفارش یافت نشد");

        return Ok(orderItem);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TOrderItems>> CreateOrderItem(CreateOrderItemDto itemDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var order = await _context.TOrders.FindAsync(itemDto.UserOrderId);
        if (order == null)
            return BadRequest("سفارش یافت نشد");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var product = await _context.TProducts.FindAsync(itemDto.ProductId);
            if (product == null)
                return BadRequest("محصول یافت نشد");

            if ((product.Count ?? 0) < itemDto.Quantity)
                return BadRequest($"موجودی کافی نیست. موجودی: {product.Count ?? 0}, درخواستی: {itemDto.Quantity}");

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

            product.Count = (product.Count ?? 0) - itemDto.Quantity;
            _context.TProducts.Update(product);

            order.TotalAmount += orderItem.Amount;
            order.TotalProfit += orderItem.Profit;
            _context.TOrders.Update(order);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return CreatedAtAction("GetOrderItem", new { id = orderItem.Id }, orderItem);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "خطا در ایجاد آیتم سفارش");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderItem(int id, UpdateOrderItemDto itemDto)
    {
        if (id <= 0)
            return BadRequest("شناسه آیتم سفارش نامعتبر است");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var orderItem = await _context.TOrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.UserOrder)
                .FirstOrDefaultAsync(oi => oi.Id == id);

            if (orderItem == null)
                return NotFound("آیتم سفارش یافت نشد");

            var oldAmount = orderItem.Amount;
            var oldProfit = orderItem.Profit;
            var oldQuantity = orderItem.Quantity;

            if (itemDto.Quantity.HasValue)
            {
                if (itemDto.Quantity.Value <= 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest("مقدار باید بیشتر از صفر باشد");
                }

                if (itemDto.Quantity.Value != oldQuantity)
                {
                    var quantityDifference = itemDto.Quantity.Value - oldQuantity;
                    var currentStock = orderItem.Product!.Count ?? 0;

                    if (currentStock < quantityDifference)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest($"موجودی کافی نیست. موجودی: {currentStock}, نیاز اضافی: {quantityDifference}");
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
                    return BadRequest("قیمت فروش باید بیشتر از صفر باشد");
                }
                orderItem.SellingPrice = itemDto.SellingPrice.Value;
            }

            orderItem.Amount = orderItem.SellingPrice * orderItem.Quantity;
            orderItem.Profit = (orderItem.SellingPrice - orderItem.PurchasePrice) * orderItem.Quantity;

            orderItem.UserOrder!.TotalAmount = orderItem.UserOrder.TotalAmount - oldAmount + orderItem.Amount;
            orderItem.UserOrder.TotalProfit = orderItem.UserOrder.TotalProfit - oldProfit + orderItem.Profit;

            _context.TOrderItems.Update(orderItem);
            _context.TOrders.Update(orderItem.UserOrder);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            if (!await OrderItemExistsAsync(id))
                return NotFound("آیتم سفارش یافت نشد");
            return Conflict("داده‌ها توسط کاربر دیگری تغییر یافته است");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "خطا در بروزرسانی آیتم سفارش");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteOrderItem(int id)
    {
        if (id <= 0)
            return BadRequest("شناسه آیتم سفارش نامعتبر است");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var orderItem = await _context.TOrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.UserOrder)
                .FirstOrDefaultAsync(oi => oi.Id == id);

            if (orderItem == null)
                return NotFound("آیتم سفارش یافت نشد");

            if (orderItem.Product != null)
            {
                orderItem.Product.Count = (orderItem.Product.Count ?? 0) + orderItem.Quantity;
                _context.TProducts.Update(orderItem.Product);
            }

            orderItem.UserOrder!.TotalAmount -= orderItem.Amount;
            orderItem.UserOrder.TotalProfit -= orderItem.Profit;
            _context.TOrders.Update(orderItem.UserOrder);

            _context.TOrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "خطا در حذف آیتم سفارش");
        }
    }

    [NonAction]
    private async Task<bool> OrderItemExistsAsync(int id)
    {
        return await _context.TOrderItems.AnyAsync(e => e.Id == id);
    }
}