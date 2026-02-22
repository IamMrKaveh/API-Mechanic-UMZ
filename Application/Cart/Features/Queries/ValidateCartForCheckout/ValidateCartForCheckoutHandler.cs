namespace Application.Cart.Features.Queries.ValidateCartForCheckout;

public class ValidateCartForCheckoutHandler
    : IRequestHandler<ValidateCartForCheckoutQuery, ServiceResult<CartCheckoutValidationDto>>
{
    private readonly ICartQueryService _cartQueryService;
    private readonly ICurrentUserService _currentUser;

    public ValidateCartForCheckoutHandler(
        ICartQueryService cartQueryService,
        ICurrentUserService currentUser)
    {
        _cartQueryService = cartQueryService;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<CartCheckoutValidationDto>> Handle(
        ValidateCartForCheckoutQuery request, CancellationToken cancellationToken)
    {
        var validation = await _cartQueryService.ValidateCartForCheckoutAsync(
            _currentUser.UserId, _currentUser.GuestId, cancellationToken);

        return ServiceResult<CartCheckoutValidationDto>.Success(validation);
    }
}