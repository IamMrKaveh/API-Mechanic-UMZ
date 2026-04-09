using Domain.Cart.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public class CheckoutCartItemBuilderService(ICartRepository cartRepository)
    : ICheckoutCartItemBuilderService
{
    public async Task<ServiceResult<CheckoutCartItemsResult>> BuildAsync(
        Guid cartId, Guid userId, CancellationToken ct)
    {
        var cart = await cartRepository.FindByIdAsync(cartId, ct);
        if (cart is null)
            return ServiceResult<CheckoutCartItemsResult>.NotFound("سبد خرید یافت نشد.");

        if (cart.UserId != userId)
            return ServiceResult<CheckoutCartItemsResult>.Forbidden("دسترسی ممنوع.");

        if (cart.IsEmpty)
            return ServiceResult<CheckoutCartItemsResult>.Failure("سبد خرید خالی است.");

        var snapshots = cart.Items.Select(item => new OrderItemSnapshot(
            item.VariantId,
            item.ProductId,
            item.ProductName.Value,
            item.Sku.Value,
            item.UnitPrice,
            item.Quantity)).ToList();

        var subtotal = cart.TotalAmount.Amount;

        return ServiceResult<CheckoutCartItemsResult>.Success(
            new CheckoutCartItemsResult(snapshots, subtotal));
    }
}