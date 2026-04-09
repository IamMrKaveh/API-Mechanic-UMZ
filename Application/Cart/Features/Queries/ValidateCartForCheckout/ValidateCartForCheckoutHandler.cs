using Application.Cart.Contracts;
using Application.Cart.Features.Shared;
using SharedKernel.Contracts;

namespace Application.Cart.Features.Queries.ValidateCartForCheckout;

public class ValidateCartForCheckoutHandler(
    ICartQueryService cartQueryService,
    ICurrentUserService currentUser)
        : IRequestHandler<ValidateCartForCheckoutQuery, ServiceResult<CartCheckoutValidationDto>>
{
    private readonly ICartQueryService _cartQueryService = cartQueryService;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<ServiceResult<CartCheckoutValidationDto>> Handle(
        ValidateCartForCheckoutQuery request,
        CancellationToken ct
        )
    {
        var validation = await _cartQueryService.ValidateCartForCheckoutAsync(
            _currentUser.UserId, _currentUser.GuestId, ct);

        return ServiceResult<CartCheckoutValidationDto>.Success(validation);
    }
}