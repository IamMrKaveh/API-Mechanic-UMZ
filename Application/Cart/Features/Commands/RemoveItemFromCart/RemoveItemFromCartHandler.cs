using Application.Cart.Features.Shared;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Cart.Features.Commands.RemoveItemFromCart;

public class RemoveItemFromCartHandler(
    ICartRepository cartRepository,
    ICartQueryService cartQueryService,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
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

        var cartResult = await (userId is not null
            ? cartRepository.FindByUserIdAsync(userId, ct)
            : cartRepository.FindByGuestTokenAsync(guestToken!, ct)).OrNotFoundAsync("سبد خرید یافت نشد.");
        if (cartResult.IsFailure) return cartResult.Error;
        var cart = cartResult.Value;

        var variantId = VariantId.From(request.VariantId);
        cart.RemoveItem(variantId, dateTimeProvider.UtcNow);
        cartRepository.Update(cart);
        await unitOfWork.SaveChangesAsync(ct);

        var cartDetail = await cartQueryService.GetCartDetailAsync(userId, guestToken, ct);

        return ServiceResult<CartDetailDto>.Success(cartDetail ?? new CartDetailDto());
    }
}