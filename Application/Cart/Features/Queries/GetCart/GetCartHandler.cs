using Application.Cart.Features.Shared;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Cart.Features.Queries.GetCart;

public class GetCartHandler(
    ICartQueryService cartQueryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetCartQuery, CartDetailDto>
{
    public async Task<ServiceResult<CartDetailDto>> Handle(
        GetCartQuery request,
        CancellationToken ct)
    {
        UserId? userId = currentUserService.UserId.HasValue
            ? UserId.From(currentUserService.UserId.Value)
            : null;

        GuestToken? guestToken = GuestToken.TryCreate(currentUserService.GuestToken);

        if (userId is null && guestToken is null)
            return ServiceResult<CartDetailDto>.Success(new CartDetailDto());

        var cart = await cartQueryService.GetCartDetailAsync(userId, guestToken, ct);

        return ServiceResult<CartDetailDto>.Success(cart ?? new CartDetailDto());
    }
}