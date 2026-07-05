using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;

namespace Application.Cart.Features.Commands.SyncCartPrices;

public class SyncCartPricesHandler(
    ICartRepository cartRepository,
    IVariantRepository variantRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<SyncCartPricesCommand>
{
    public async Task<ServiceResult> Handle(SyncCartPricesCommand request, CancellationToken ct)
    {
        Domain.Cart.Aggregates.Cart? cart;
        var userId = UserId.From(currentUserService.UserId.Value);
        var guestToken = GuestToken.Create(currentUserService.GuestToken);

        if (currentUserService.UserId.HasValue)
        {
            cart = await cartRepository.FindByUserIdAsync(userId, ct);
        }
        else if (!string.IsNullOrWhiteSpace(currentUserService.GuestToken))
        {
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
        await unitOfWork.SaveChangesAsync(ct);

        if (userId is not null)
        {
            await auditService.LogAsync("Cart", "SyncCartPrices", IpAddress.Unknown, userId, entityType: "Cart", ct: ct);
        }

        return ServiceResult.Success();
    }
}