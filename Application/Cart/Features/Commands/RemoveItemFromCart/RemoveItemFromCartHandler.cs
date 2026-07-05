using Application.Cart.Features.Shared;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Cart.Features.Commands.RemoveItemFromCart;

public class RemoveItemFromCartHandler(
    ICartRepository cartRepository,
    ICartQueryService cartQueryService,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : ICommandHandler<RemoveItemFromCartCommand, CartDetailDto>
{
    public async Task<ServiceResult<CartDetailDto>> Handle(
        RemoveItemFromCartCommand request,
        CancellationToken ct)
    {
        UserId? userId = currentUserService.UserId.HasValue ? UserId.From(currentUserService.UserId.Value) : null;
        GuestToken? guestToken = GuestToken.TryCreate(currentUserService.GuestToken);

        if (userId is null && guestToken is null)
            return ServiceResult<CartDetailDto>.Validation("UserId یا GuestToken الزامی است.");

        Domain.Cart.Aggregates.Cart? cart = userId is not null
            ? await cartRepository.FindByUserIdAsync(userId, ct)
            : await cartRepository.FindByGuestTokenAsync(guestToken!, ct);

        if (cart is null)
            return ServiceResult<CartDetailDto>.NotFound("سبد خرید یافت نشد.");

        var variantId = VariantId.From(request.VariantId);
        cart.RemoveItem(variantId);
        cartRepository.Update(cart);
        await unitOfWork.SaveChangesAsync(ct);

        var cartDetail = await cartQueryService.GetCartDetailAsync(userId, guestToken, ct);

        return ServiceResult<CartDetailDto>.Success(cartDetail ?? new CartDetailDto());
    }
}