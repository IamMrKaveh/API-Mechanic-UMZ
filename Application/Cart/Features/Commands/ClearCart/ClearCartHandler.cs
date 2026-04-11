using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Cart.Features.Commands.ClearCart;

public class ClearCartHandler(
    ICartRepository cartRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ClearCartCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ClearCartCommand request, CancellationToken ct)
    {
        Domain.Cart.Aggregates.Cart? cart;

        if (request.UserId.HasValue)
        {
            cart = await cartRepository.FindByUserIdAsync(UserId.From(request.UserId.Value), ct);
        }
        else
        {
            var guestToken = GuestToken.TryCreate(request.GuestToken);
            if (guestToken is null)
                return ServiceResult.Failure("توکن مهمان نامعتبر است.", SharedKernel.Results.ErrorType.Validation);

            cart = await cartRepository.FindByGuestTokenAsync(guestToken, ct);
        }

        if (cart is null)
            return ServiceResult.Success();

        cart.Clear();
        cartRepository.Update(cart);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}