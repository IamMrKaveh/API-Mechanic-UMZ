using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.Inventory.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Order.Services;

public class CheckoutStockValidatorService(IInventoryRepository inventoryRepository)
    : ICheckoutStockValidatorService
{
    public async Task<ServiceResult> ValidateAsync(List<OrderItemSnapshot> items, CancellationToken ct)
    {
        var errors = new List<string>();

        foreach (var item in items)
        {
            var inventory = await inventoryRepository.GetByVariantIdAsync(
                VariantId.From(item.VariantId), ct);

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