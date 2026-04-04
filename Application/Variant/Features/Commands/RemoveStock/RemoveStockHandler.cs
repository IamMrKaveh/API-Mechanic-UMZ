using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Application.Common.Results;
using Application.Inventory.Contracts;
using Domain.Common.Interfaces;
using Domain.Variant.Interfaces;

namespace Application.Variant.Features.Commands.RemoveStock;

public class RemoveStockHandler(
    IVariantRepository variantRepository,
    IInventoryService inventoryService,
    IAuditService auditService,
    IUnitOfWork unitOfWork,
    ICacheService cacheService) : IRequestHandler<RemoveStockCommand, ServiceResult>
{
    private readonly IVariantRepository _variantRepository = variantRepository;
    private readonly IInventoryService _inventoryService = inventoryService;
    private readonly IAuditService _auditService = auditService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICacheService _cacheService = cacheService;

    public async Task<ServiceResult> Handle(
        RemoveStockCommand request,
        CancellationToken ct)
    {
        var variant = await _variantRepository.GetByIdAsync(request.VariantId, ct);
        if (variant == null)
            return ServiceResult.NotFound("Variant not found.");

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
            ct: ct);

        variant.AdjustStock(-request.Quantity);
        _variantRepository.Update(variant);

        await _unitOfWork.SaveChangesAsync(ct);
        await _auditService.LogInventoryEventAsync(
            variant.ProductId,
            "RemoveStock",
            $"Removed {request.Quantity} from stock for variant {request.VariantId}.",
            request.UserId.Value);

        await _cacheService.ClearAsync($"product:{variant.ProductId}");
        await _cacheService.ClearAsync($"variant:{request.VariantId}");

        return ServiceResult.Success();
    }
}