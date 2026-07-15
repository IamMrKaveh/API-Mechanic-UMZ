using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Cart.Features.Commands.ClearCart;

public class ClearCartHandler(
    ICartRepository cartRepository,
    ICurrentUserService currentUserService)
    : ICommandHandler<ClearCartCommand>
{
    public async Task<ServiceResult> Handle(ClearCartCommand request, CancellationToken ct)
    {
        Domain.Cart.Aggregates.Cart? cart;

        if (currentUserService.UserId.HasValue)
        {
            cart = await cartRepository.FindByUserIdAsync(UserId.From(currentUserService.UserId.Value), ct);
        }
        else
        {
            var guestToken = GuestToken.TryCreate(currentUserService.GuestToken);
            if (guestToken is null)
                return ServiceResult.Failure("توکن مهمان نامعتبر است.", SharedKernel.Results.ErrorType.Validation);

            cart = await cartRepository.FindByGuestTokenAsync(guestToken, ct);
        }

        if (cart is null)
            return ServiceResult.Success();

        cart.Clear();
        cartRepository.Update(cart);

        return ServiceResult.Success();
    }
}