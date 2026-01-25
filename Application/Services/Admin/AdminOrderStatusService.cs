namespace Application.Services.Admin;

public class AdminOrderStatusService : IAdminOrderStatusService
{
    private readonly IOrderStatusRepository _orderStatusRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AdminOrderStatusService> _logger;
    private readonly IHtmlSanitizer _htmlSanitizer;

    private const string OrderStatusesCacheKey = "order_statuses:all";

    public AdminOrderStatusService(
        IOrderStatusRepository orderStatusRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<AdminOrderStatusService> logger,
        IHtmlSanitizer htmlSanitizer)
    {
        _orderStatusRepository = orderStatusRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<IEnumerable<Domain.Order.OrderStatus>> GetOrderStatusesAsync()
    {
        return await _orderStatusRepository.GetOrderStatusesAsync();
    }

    public async Task<OrderStatus?> GetOrderStatusByIdAsync(int id)
    {
        return await _orderStatusRepository.GetOrderStatusByIdAsync(id);
    }

    public async Task<OrderStatus> CreateOrderStatusAsync(CreateOrderStatusDto statusDto)
    {
        if (string.IsNullOrWhiteSpace(statusDto.Name))
        {
            throw new ArgumentException("Status name cannot be empty.", nameof(statusDto.Name));
        }

        var status = new OrderStatus
        {
            Name = _htmlSanitizer.Sanitize(statusDto.Name),
            Icon = _htmlSanitizer.Sanitize(statusDto.Icon ?? string.Empty)
        };
        await _orderStatusRepository.AddOrderStatusAsync(status);
        await _unitOfWork.SaveChangesAsync();

        await InvalidateOrderStatusCacheAsync();

        _logger.LogInformation("New order status created: {StatusName} (ID: {StatusId})", status.Name, status.Id);
        return status;
    }

    public async Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto statusDto)
    {
        var status = await _orderStatusRepository.GetOrderStatusByIdForUpdateAsync(id);
        if (status == null) return false;

        if (!string.IsNullOrWhiteSpace(statusDto.Name))
            status.Name = _htmlSanitizer.Sanitize(statusDto.Name);

        if (statusDto.Icon != null)
            status.Icon = _htmlSanitizer.Sanitize(statusDto.Icon);

        _orderStatusRepository.UpdateOrderStatus(status);
        await _unitOfWork.SaveChangesAsync();

        await InvalidateOrderStatusCacheAsync();

        _logger.LogInformation("Order status updated: {StatusName} (ID: {StatusId})", status.Name, status.Id);
        return true;
    }

    public async Task<bool> DeleteOrderStatusAsync(int id)
    {
        var status = await _orderStatusRepository.GetOrderStatusByIdForUpdateAsync(id);
        if (status == null) return false;

        var isUsed = await _orderStatusRepository.IsOrderStatusInUseAsync(id);
        if (isUsed)
        {
            _logger.LogWarning("Attempted to delete an order status that is in use: ID {StatusId}", id);
            throw new InvalidOperationException("Cannot delete order status because it is currently in use by one or more orders.");
        }

        status.IsDeleted = true;
        status.DeletedAt = DateTime.UtcNow;

        _orderStatusRepository.UpdateOrderStatus(status);
        await _unitOfWork.SaveChangesAsync();

        await InvalidateOrderStatusCacheAsync();

        _logger.LogInformation("Order status deleted: ID {StatusId}", id);
        return true;
    }

    private async Task InvalidateOrderStatusCacheAsync()
    {
        await _cacheService.ClearAsync(OrderStatusesCacheKey);
        _logger.LogDebug("Order status cache invalidated");
    }
}