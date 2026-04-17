using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.Order.ValueObjects;

namespace Infrastructure.Order.Services;

public sealed class CheckoutCartItemBuilderService(ICartRepository cartRepository)
    : ICheckoutCartItemBuilderService
{
    public async Task<ServiceResult<CheckoutCartItemsResult>> BuildAsync(
        Guid cartId,
        Guid userId,
        CancellationToken ct)
    {
        var cart = await cartRepository.FindByIdAsync(CartId.From(cartId), ct);
        if (cart is null)
            return ServiceResult<CheckoutCartItemsResult>.NotFound("سبد خرید یافت نشد.");

        if (cart.UserId.Value != userId)
            return ServiceResult<CheckoutCartItemsResult>.Forbidden("دسترسی ممنوع.");

        if (cart.IsEmpty)
            return ServiceResult<CheckoutCartItemsResult>.Failure("سبد خرید خالی است.");

        var snapshots = cart.CartItems.Select(item => OrderItemSnapshot.Create(
            item.VariantId,
            item.ProductId,
            item.ProductName,
            item.Sku,
            item.SellingPrice,
            item.Quantity)).ToList();

        var subtotal = cart.TotalAmount.Amount;

        return ServiceResult<CheckoutCartItemsResult>.Success(
            new CheckoutCartItemsResult(snapshots, subtotal));
    }
}