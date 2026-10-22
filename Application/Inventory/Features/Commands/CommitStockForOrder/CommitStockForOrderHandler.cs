using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Inventory.Features.Commands.CommitStockForOrder;

/// <summary>
/// Turns the reserved stock of an order into a stock movement.
///
/// The command is idempotent on its own: the inventory aggregate checks its
/// ledger and ignores a commit it already holds, so a republished outbox
/// message never takes the stock out a second time. Nothing else has to
/// guard the step against a duplicate.
/// </summary>
public class CommitStockForOrderHandler(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CommitStockForOrderCommand>
{
    public async Task<ServiceResult> Handle(CommitStockForOrderCommand request, CancellationToken ct)
    {
        if (request.Items.Count == 0)
            return ServiceResult.Failure("لیست اقلام سفارش خالی است.");

        await unitOfWork.ExecuteStrategyAsync(async cancellationToken =>
        {
            foreach (var item in request.Items)
            {
                var variantId = VariantId.From(item.VariantId);

                var inventory = await inventoryRepository.GetByVariantIdAsync(
                    variantId,
                    cancellationToken) ?? throw new DomainException(
                        $"موجودی واریانت {item.VariantId} یافت نشد.");
                var quantity = StockQuantity.Create(item.Quantity);

                var orderItemId = item.OrderItemId.HasValue
                    ? OrderItemId.From(item.OrderItemId.Value)
                    : null;

                var result = InventoryDomainService.ConfirmReservation(
                    inventory,
                    quantity,
                    request.OrderNumber,
                    dateTimeProvider.UtcNow,
                    orderItemId);

                if (result.IsFailure)
                    throw new DomainException(result.Error.Message);

                inventoryRepository.Update(inventory);
            }

            await auditService.LogInventoryEventAsync(
                VariantId.From(request.Items[0].VariantId),
                "CommitStockForOrder",
                $"تأیید رزرو موجودی برای سفارش {request.OrderNumber}، {request.Items.Count} قلم");

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return 0;
        }, ct);

        return ServiceResult.Success();
    }
}