namespace Application.Cart.Features.Queries.GetCart;

public class GetCartHandler : IRequestHandler<GetCartQuery, ServiceResult<CartDetailDto>>
{
    private readonly ICartQueryService _cartQueryService;
    private readonly ICurrentUserService _currentUser;

    public GetCartHandler(
        ICartQueryService cartQueryService,
        ICurrentUserService currentUser
        )
    {
        _cartQueryService = cartQueryService;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<CartDetailDto>> Handle(
        GetCartQuery request,
        CancellationToken ct
        )
    {
        var cart = await _cartQueryService.GetCartDetailAsync(
            _currentUser.UserId, _currentUser.GuestId, ct);

        if (cart == null)
        {
            return ServiceResult<CartDetailDto>.Success(new CartDetailDto
            {
                Id = 0,
                UserId = _currentUser.UserId,
                GuestToken = _currentUser.GuestId,
                Items = new List<CartItemDetailDto>(),
                TotalPrice = 0,
                TotalItems = 0,
                PriceChanges = new List<CartPriceChangeDto>()
            });
        }

        return ServiceResult<CartDetailDto>.Success(cart);
    }
}