namespace Application.Services;

public class OrderStatusService : IOrderStatusService
{
    private readonly IOrderStatusRepository _orderStatusRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderStatusService> _logger;
    private readonly IHtmlSanitizer _htmlSanitizer;

    public OrderStatusService(
        IOrderStatusRepository orderStatusRepository,
        IUnitOfWork unitOfWork,
        ILogger<OrderStatusService> logger,
        IHtmlSanitizer htmlSanitizer)
    {
        _orderStatusRepository = orderStatusRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<IEnumerable<Domain.Order.OrderStatus>> GetOrderStatusesAsync()
    {
        return await _orderStatusRepository.GetOrderStatusesAsync();
    }

    public async Task<Domain.Order.OrderStatus?> GetOrderStatusByIdAsync(int id)
    {
        return await _orderStatusRepository.GetOrderStatusByIdAsync(id);
    }

    public async Task<Domain.Order.OrderStatus> CreateOrderStatusAsync(CreateOrderStatusDto statusDto)
    {
        if (string.IsNullOrWhiteSpace(statusDto.Name))
        {
            throw new ArgumentException("Status name cannot be empty.", nameof(statusDto.Name));
        }

        var status = new Domain.Order.OrderStatus
        {
            Name = _htmlSanitizer.Sanitize(statusDto.Name)
        };
        await _orderStatusRepository.AddOrderStatusAsync(status);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("New order status created: {StatusName} (ID: {StatusId})", status.Name, status.Id);
        return status;
    }

    public async Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto statusDto)
    {
        var status = await _orderStatusRepository.GetOrderStatusByIdForUpdateAsync(id);
        if (status == null) return false;

        if (!string.IsNullOrWhiteSpace(statusDto.Name))
            status.Name = _htmlSanitizer.Sanitize(statusDto.Name);

        await _unitOfWork.SaveChangesAsync();
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

        _orderStatusRepository.DeleteOrderStatus(status);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Order status deleted: ID {StatusId}", id);
        return true;
    }
}