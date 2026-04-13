using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.Common.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;

namespace Application.Cart.Features.Commands.SyncCartPrices;

public class SyncCartPricesHandler(
    ICartRepository cartRepository,
    IVariantRepository variantRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<SyncCartPricesCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(SyncCartPricesCommand request, CancellationToken ct)
    {
        Domain.Cart.Aggregates.Cart? cart;

        if (request.UserId.HasValue)
        {
            var userId = UserId.From(request.UserId.Value);
            cart = await cartRepository.FindByUserIdAsync(userId, ct);
        }
        else if (!string.IsNullOrWhiteSpace(request.GuestToken))
        {
            var guestToken = GuestToken.Create(request.GuestToken);
            cart = await cartRepository.FindByGuestTokenAsync(guestToken, ct);
        }
        else
        {
            return ServiceResult.NotFound("سبد خرید یافت نشد.");
        }

        if (cart is null)
            return ServiceResult.NotFound("سبد خرید یافت نشد.");

        foreach (var item in cart.Items)
        {
            var variant = await variantRepository.GetByIdAsync(item.VariantId, ct);
            if (variant is not null)
                cart.RefreshItemPrice(item.VariantId, variant.Price, variant.CompareAtPrice ?? variant.Price);
        }

        cartRepository.Update(cart);
        await unitOfWork.SaveChangesAsync(ct);

        if (request.UserId.HasValue)
        {
            var userId = UserId.From(request.UserId.Value);
            await auditService.LogAsync("Cart", "SyncCartPrices", IpAddress.Unknown, userId, entityType: "Cart", ct: ct);
        }

        return ServiceResult.Success();
    }
}