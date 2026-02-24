namespace Application.Inventory.Features.Commands.CommitStockForOrder;

public class CommitStockForOrderHandler : IRequestHandler<CommitStockForOrderCommand, ServiceResult>
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<CommitStockForOrderHandler> _logger;

    public CommitStockForOrderHandler(
        IInventoryService inventoryService,
        ILogger<CommitStockForOrderHandler> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        CommitStockForOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Committing stock for Order {OrderId}", request.OrderId);

        var result = await _inventoryService.CommitStockForOrderAsync(request.OrderId, cancellationToken);

        if (result.IsFailed)
        {
            _logger.LogError("Failed to commit stock for Order {OrderId}: {Error}",
                request.OrderId, result.Error);
        }

        return result;
    }
}