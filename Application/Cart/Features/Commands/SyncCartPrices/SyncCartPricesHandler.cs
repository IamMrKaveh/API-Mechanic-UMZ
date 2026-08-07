using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;

namespace Application.Cart.Features.Commands.SyncCartPrices;

public class SyncCartPricesHandler(
    ICartRepository cartRepository,
    IVariantRepository variantRepository,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<SyncCartPricesCommand>
{
    public async Task<ServiceResult> Handle(SyncCartPricesCommand request, CancellationToken ct)
    {
        Domain.Cart.Aggregates.Cart? cart;
        UserId? userId = currentUserService.UserId.HasValue
            ? UserId.From(currentUserService.UserId.Value)
            : null;

        if (userId is not null)
        {
            cart = await cartRepository.FindByUserIdAsync(userId, ct);
        }
        else if (!string.IsNullOrWhiteSpace(currentUserService.GuestToken))
        {
            var guestToken = GuestToken.Create(currentUserService.GuestToken);
            cart = await cartRepository.FindByGuestTokenAsync(guestToken, ct);
        }
        else
        {
            return ServiceResult.NotFound("سبد خرید یافت نشد.");
        }

        if (cart is null)
            return ServiceResult.NotFound("سبد خرید یافت نشد.");

        foreach (var item in cart.CartItems)
        {
            var variant = await variantRepository.GetByIdAsync(item.VariantId, ct);
            if (variant is not null)
                cart.RefreshItemPrice(item.VariantId, variant.SellingPrice, variant.OriginalPrice);
        }

        cartRepository.Update(cart);

        if (userId is not null)
        {
            await auditService.LogAsync("Cart", "SyncCartPrices", IpAddress.Unknown, userId, entityType: "Cart", ct: ct);
        }

        return ServiceResult.Success();
    }
}
