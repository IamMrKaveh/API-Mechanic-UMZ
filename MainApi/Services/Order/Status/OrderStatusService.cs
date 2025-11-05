namespace MainApi.Services.Order.Status;

public class OrderStatusService : IOrderStatusService
{
    private readonly MechanicContext _context;
    private readonly ILogger<OrderStatusService> _logger;
    private readonly IHtmlSanitizer _htmlSanitizer;

    public OrderStatusService(MechanicContext context, ILogger<OrderStatusService> logger, IHtmlSanitizer htmlSanitizer)
    {
        _context = context;
        _logger = logger;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<IEnumerable<TOrderStatus>> GetOrderStatusesAsync()
    {
        return await _context.TOrderStatus.AsNoTracking().OrderBy(s => s.Id).ToListAsync();
    }

    public async Task<TOrderStatus?> GetOrderStatusByIdAsync(int id)
    {
        return await _context.TOrderStatus.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<TOrderStatus> CreateOrderStatusAsync(CreateOrderStatusDto statusDto)
    {
        if (string.IsNullOrWhiteSpace(statusDto.Name))
        {
            throw new ArgumentException("Status name cannot be empty.", nameof(statusDto.Name));
        }

        var status = new TOrderStatus
        {
            Name = _htmlSanitizer.Sanitize(statusDto.Name),
            Icon = _htmlSanitizer.Sanitize(statusDto.Icon ?? string.Empty)
        };
        _context.TOrderStatus.Add(status);
        await _context.SaveChangesAsync();
        _logger.LogInformation("New order status created: {StatusName} (ID: {StatusId})", status.Name, status.Id);
        return status;
    }

    public async Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto statusDto)
    {
        var status = await _context.TOrderStatus.FindAsync(id);
        if (status == null) return false;

        if (!string.IsNullOrWhiteSpace(statusDto.Name))
            status.Name = _htmlSanitizer.Sanitize(statusDto.Name);

        if (statusDto.Icon != null)
            status.Icon = _htmlSanitizer.Sanitize(statusDto.Icon);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Order status updated: {StatusName} (ID: {StatusId})", status.Name, status.Id);
        return true;
    }

    public async Task<bool> DeleteOrderStatusAsync(int id)
    {
        var status = await _context.TOrderStatus.FindAsync(id);
        if (status == null) return false;

        var isUsed = await _context.TOrders.AnyAsync(o => o.OrderStatusId == id);
        if (isUsed)
        {
            _logger.LogWarning("Attempted to delete an order status that is in use: ID {StatusId}", id);
            throw new InvalidOperationException("Cannot delete order status because it is currently in use by one or more orders.");
        }

        _context.TOrderStatus.Remove(status);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Order status deleted: ID {StatusId}", id);
        return true;
    }
}