using Application.Cart.Features.Shared;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Cart.Features.Queries.ValidateCartForCheckout;

public class ValidateCartForCheckoutHandler(
    ICartQueryService cartQueryService) : IRequestHandler<ValidateCartForCheckoutQuery, ServiceResult<CartCheckoutValidationDto>>
{
    public async Task<ServiceResult<CartCheckoutValidationDto>> Handle(
        ValidateCartForCheckoutQuery request,
        CancellationToken ct)
    {
        UserId? userId = request.UserId.HasValue ? UserId.From(request.UserId.Value) : null;
        GuestToken? guestToken = GuestToken.TryCreate(request.GuestToken);

        if (userId is null && guestToken is null)
            return ServiceResult<CartCheckoutValidationDto>.Validation("UserId یا GuestToken الزامی است.");

        var validation = await cartQueryService.ValidateCartForCheckoutAsync(userId, guestToken, ct);

        return ServiceResult<CartCheckoutValidationDto>.Success(validation);
    }
}