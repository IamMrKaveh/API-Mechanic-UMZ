using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.Inventory.Interfaces;
using Domain.Order.ValueObjects;

namespace Infrastructure.Order.Services;

public sealed class CheckoutStockValidatorService(IInventoryRepository inventoryRepository)
    : ICheckoutStockValidatorService
{
    public async Task<ServiceResult> ValidateAsync(
        IReadOnlyCollection<OrderItemSnapshot> items, CancellationToken ct)
    {
        var errors = new List<string>();

        // Nothing to validate: skip the round-trip entirely.
        if (items.Count == 0)
            return ServiceResult.Success();

        // One batched query for the whole cart instead of one query per item.
        var variantIds = items.Select(i => i.VariantId).Distinct().ToList();
        var inventories = await inventoryRepository.GetByVariantIdsAsync(variantIds, ct);

        foreach (var item in items)
        {
            var inventory = inventories.FirstOrDefault(i => i.VariantId == item.VariantId);

            if (inventory is null)
            {
                errors.Add($"موجودی واریانت {item.VariantId} یافت نشد.");
                continue;
            }

            if (!inventory.CanFulfill(item.Quantity))
                errors.Add($"موجودی کافی برای محصول {item.ProductName} وجود ندارد.");
        }

        return errors.Count > 0
            ? ServiceResult.Failure(string.Join(" | ", errors))
            : ServiceResult.Success();
    }
}