using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Commands.BulkStockIn;

public class BulkStockInHandler(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : IRequestHandler<BulkStockInCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(BulkStockInCommand request, CancellationToken ct)
    {
        if (request.Items.Count == 0)
            return ServiceResult.Failure("لیست اقلام برای ورود موجودی خالی است.");

        var userId = currentUserService.UserId.HasValue
            ? UserId.From(currentUserService.UserId.Value)
            : null;

        await unitOfWork.ExecuteStrategyAsync(async cancellationToken =>
        {
            foreach (var item in request.Items)
            {
                var variantId = VariantId.From(item.VariantId);

                var inventory = await inventoryRepository.GetByVariantIdAsync(
                    variantId,
                    cancellationToken) ?? throw new DomainException(
                        $"موجودی برای واریانت {item.VariantId} یافت نشد.");
                var stockQuantity = StockQuantity.Create(item.Quantity);

                var result = InventoryDomainService.IncreaseStock(
                    inventory,
                    stockQuantity,
                    request.Reason,
                    userId);

                if (result.IsFailure)
                    throw new DomainException(result.Error.Message);

                inventoryRepository.Update(inventory);
            }

            if (userId is not null)
            {
                await auditService.LogInventoryEventAsync(
                    VariantId.From(request.Items[0].VariantId),
                    "BulkStockIn",
                    $"ورود دسته‌ای موجودی برای {request.Items.Count} واریانت. دلیل: {request.Reason}",
                    userId);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return 0;
        }, ct);

        return ServiceResult.Success();
    }
}