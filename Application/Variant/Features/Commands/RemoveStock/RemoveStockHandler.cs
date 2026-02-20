namespace Application.Variant.Features.Commands.RemoveStock;

public class RemoveStockHandler : IRequestHandler<RemoveStockCommand, ServiceResult>
{
    private readonly IVariantRepository _variantRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public RemoveStockHandler(
        IVariantRepository variantRepository,
        IInventoryService inventoryService,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _variantRepository = variantRepository;
        _inventoryService = inventoryService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<ServiceResult> Handle(RemoveStockCommand request, CancellationToken cancellationToken)
    {
        var variant = await _variantRepository.GetByIdAsync(request.VariantId, cancellationToken);
        if (variant == null) return ServiceResult.Failure("Variant not found.");

        await _inventoryService.LogTransactionAsync(
            request.VariantId,
            "StockOut",
            -request.Quantity,
            null,
            request.UserId,
            request.Notes,
            null,
            variant.StockQuantity,
            saveChanges: false,
            ct: cancellationToken);

        variant.AdjustStock(-request.Quantity);
        _variantRepository.Update(variant);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditService.LogInventoryEventAsync(variant.ProductId, "RemoveStock", $"Removed {request.Quantity} from stock for variant {request.VariantId}.", request.UserId);

        await _cacheService.ClearAsync($"product:{variant.ProductId}");
        await _cacheService.ClearAsync($"variant:{request.VariantId}");

        return ServiceResult.Success();
    }
}