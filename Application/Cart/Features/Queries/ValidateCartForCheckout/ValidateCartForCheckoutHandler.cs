using Application.Cart.Features.Shared;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Cart.Features.Queries.ValidateCartForCheckout;

public class ValidateCartForCheckoutHandler(
    ICartQueryService cartQueryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<ValidateCartForCheckoutQuery, CartCheckoutValidationDto>
{
    public async Task<ServiceResult<CartCheckoutValidationDto>> Handle(
        ValidateCartForCheckoutQuery request,
        CancellationToken ct)
    {
        UserId? userId = currentUserService.UserId.HasValue ? UserId.From(currentUserService.UserId.Value) : null;
        GuestToken? guestToken = GuestToken.TryCreate(currentUserService.GuestToken);

        if (userId is null && guestToken is null)
            return ServiceResult<CartCheckoutValidationDto>.Validation("UserId یا GuestToken الزامی است.");

        var validation = await cartQueryService.ValidateCartForCheckoutAsync(userId, guestToken, ct);

        return ServiceResult<CartCheckoutValidationDto>.Success(validation);
    }
}