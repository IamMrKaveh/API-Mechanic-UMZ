using Application.Cart.Features.Shared;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Cart.Features.Queries.GetCart;

public class GetCartHandler(
    ICartQueryService cartQueryService) : IRequestHandler<GetCartQuery, ServiceResult<CartDetailDto>>
{
    public async Task<ServiceResult<CartDetailDto>> Handle(
        GetCartQuery request,
        CancellationToken ct)
    {
        UserId? userId = request.UserId.HasValue ? UserId.From(request.UserId.Value) : null;
        GuestToken? guestToken = GuestToken.TryCreate(request.GuestToken);

        if (userId is null && guestToken is null)
            return ServiceResult<CartDetailDto>.Validation("UserId یا GuestToken الزامی است.");

        var cart = await cartQueryService.GetCartDetailAsync(userId, guestToken, ct);

        return ServiceResult<CartDetailDto>.Success(cart ?? new CartDetailDto());
    }
}