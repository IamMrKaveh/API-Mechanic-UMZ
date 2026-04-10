using Application.Cart.Features.Shared;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Cart.Features.Queries.ValidateCartForCheckout;

public class ValidateCartForCheckoutHandler(ICartQueryService cartQueryService) : IRequestHandler<ValidateCartForCheckoutQuery, ServiceResult<CartCheckoutValidationDto>>
{
    private readonly ICartQueryService _cartQueryService = cartQueryService;

    public async Task<ServiceResult<CartCheckoutValidationDto>> Handle(
        ValidateCartForCheckoutQuery request,
        CancellationToken ct)
    {
        var userId = request.UserId.HasValue ? UserId.From(request.UserId.Value) : null;

        var guestToken = GuestToken.Create(request.GuestToken);

        var validation = await _cartQueryService.ValidateCartForCheckoutAsync(
            userId, guestToken, ct);

        return ServiceResult<CartCheckoutValidationDto>.Success(validation);
    }
}