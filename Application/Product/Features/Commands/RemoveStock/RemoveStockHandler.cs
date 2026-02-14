namespace Application.Product.Features.Commands.RemoveStock;

public class RemoveStockHandler : IRequestHandler<RemoveStockCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public RemoveStockHandler(
        IProductRepository productRepository,
        IInventoryService inventoryService,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _inventoryService = inventoryService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<ServiceResult> Handle(RemoveStockCommand request, CancellationToken cancellationToken)
    {
        var variant = await _productRepository.GetVariantByIdAsync(request.VariantId);
        if (variant == null) return ServiceResult.Failure("Variant not found.");

        await _inventoryService.LogTransactionAsync(
            request.VariantId,
            "StockOut",
            -request.Quantity,
            null,
            request.UserId,
            request.Notes,
            null,
            Convert.ToInt32(variant.RowVersion),
            saveChanges: false);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditService.LogInventoryEventAsync(variant.ProductId, "RemoveStock", $"Removed {request.Quantity} from stock for variant {request.VariantId}.", request.UserId);

        await _cacheService.ClearAsync($"product:{variant.ProductId}");

        return ServiceResult.Success();
    }
}