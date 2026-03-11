using Application.Cart;
using Domain.Variant.Aggregates;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public sealed class CheckoutCartItemBuilderService(ICartRepository cartRepository) : ICheckoutCartItemBuilderService
{
    private readonly ICartRepository _cartRepository = cartRepository;

    public async Task<ServiceResult<CheckoutCartItemsResult>> BuildAsync(Domain.Cart.Aggregates.Cart cart, CancellationToken ct)
    {
        var orderItemSnapshots = new List<OrderItemSnapshot>();
        var variantItems = new List<(ProductVariant Variant, int Quantity)>();

        foreach (var cartItem in cart.CartItems)
        {
            var variant = await _cartRepository.GetVariantByIdAsync(cartItem.VariantId, ct);
            if (variant == null || !variant.IsActive)
                return ServiceResult<CheckoutCartItemsResult>.Failure("محصول ناشناخته یا غیرفعال در سبد خرید وجود دارد.");

            variantItems.Add((variant, cartItem.Quantity));
            orderItemSnapshots.Add(OrderItemSnapshot.FromVariant(variant, cartItem.Quantity));
        }

        return ServiceResult<CheckoutCartItemsResult>.Success(new CheckoutCartItemsResult
        {
            OrderItemSnapshots = orderItemSnapshots.AsReadOnly(),
            VariantItems = variantItems.AsReadOnly()
        });
    }
}