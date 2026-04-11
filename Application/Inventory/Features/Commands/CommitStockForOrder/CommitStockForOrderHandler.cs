using Application.Audit.Contracts;
using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Commands.CommitStockForOrder;

public class CommitStockForOrderHandler(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<CommitStockForOrderCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(CommitStockForOrderCommand request, CancellationToken ct)
    {
        using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var errors = new List<string>();

            foreach (var item in request.Items)
            {
                var variantId = VariantId.From(item.VariantId);
                var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);

                if (inventory is null)
                {
                    errors.Add($"موجودی واریانت {item.VariantId} یافت نشد.");
                    continue;
                }

                var stock = StockQuantity.Create(item.Quantity);
                var orderItemId = item.OrderItemId.HasValue ? OrderItemId.From(item.OrderItemId.Value) : null;

                var result = InventoryDomainService.ConfirmReservation(
                    inventory,
                    stock,
                    request.OrderNumber,
                    orderItemId);

                if (result.IsFailure)
                {
                    errors.Add($"واریانت {item.VariantId}: {result.Error.Message}");
                    continue;
                }

                inventoryRepository.Update(inventory);
            }

            if (errors.Count > 0)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return ServiceResult.Failure(string.Join(" | ", errors));
            }

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);

            await auditService.LogInventoryEventAsync(
                VariantId.From(request.Items[0].VariantId),
                "CommitStockForOrder",
                $"تأیید رزرو موجودی برای سفارش {request.OrderNumber}، {request.Items.Count} قلم");

            return ServiceResult.Success();
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }
}