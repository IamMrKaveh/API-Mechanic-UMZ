using Application.Cart.Features.Shared;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Cart.Features.Queries.GetCartSummary;

public class GetCartSummaryHandler(ICartQueryService cartQueryService) : IRequestHandler<GetCartSummaryQuery, ServiceResult<CartSummaryDto>>
{
    private readonly ICartQueryService _cartQueryService = cartQueryService;

    public async Task<ServiceResult<CartSummaryDto>> Handle(
        GetCartSummaryQuery request,
        CancellationToken ct)
    {
        var guestToken = GuestToken.Create(request.GuestToken);

        var userId = UserId.From(request.UserId);

        var summary = await _cartQueryService.GetCartSummaryAsync(
            userId,
            guestToken,
            ct);

        return ServiceResult<CartSummaryDto>.Success(summary);
    }
}