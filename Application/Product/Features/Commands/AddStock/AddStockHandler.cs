using Application.Audit.Contracts;

namespace Application.Product.Features.Commands.AddStock;

public class AddStockHandler : IRequestHandler<AddStockCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public AddStockHandler(IProductRepository productRepository, IInventoryService inventoryService, IAuditService auditService, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _inventoryService = inventoryService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(AddStockCommand request, CancellationToken cancellationToken)
    {
        var variant = await _productRepository.GetVariantByIdForUpdateAsync(request.VariantId);
        if (variant == null) return ServiceResult.Failure("Variant not found.");

        variant.AddStock(request.Quantity);

        await _inventoryService.LogTransactionAsync(
            request.VariantId,
            "StockIn",
            request.Quantity,
            null,
            request.UserId,
            request.Notes,
            null,
            null,
            saveChanges: false
        );

        await _auditService.LogInventoryEventAsync(variant.ProductId, "AddStock", $"Added {request.Quantity}.", request.UserId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }
}