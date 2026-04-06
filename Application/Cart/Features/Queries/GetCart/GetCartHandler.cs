using Application.Cart.Contracts;
using Application.Cart.Features.Shared;
using Application.Common.Results;
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
        UserId? userId = request.UserId.HasValue ? UserId.From(request.UserId.Value) : null;
        var cart = await _cartQueryService.GetCartDetailAsync(userId, request.GuestToken, ct);

        return ServiceResult<CartDetailDto>.Success(cart ?? new CartDetailDto());
    }
}