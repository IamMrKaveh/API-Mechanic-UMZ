using Application.Cart.Features.Shared;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Cart.Features.Queries.GetCart;

public class GetCartHandler(
    ICartQueryService cartQueryService) : IRequestHandler<GetCartQuery, ServiceResult<CartDetailDto>>
{
    private readonly ICartQueryService _cartQueryService = cartQueryService;

    public async Task<ServiceResult<CartDetailDto>> Handle(
        GetCartQuery request,
        CancellationToken ct)
    {
        var userId = request.UserId.HasValue ? UserId.From(request.UserId.Value) : null;

        var guestToken = GuestToken.Create(request.GuestToken);

        var cart = await _cartQueryService.GetCartDetailAsync(userId, guestToken, ct);

        return ServiceResult<CartDetailDto>.Success(cart ?? new CartDetailDto());
    }
}