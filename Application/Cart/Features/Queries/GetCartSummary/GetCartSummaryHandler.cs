using Application.Cart.Features.Shared;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Cart.Features.Queries.GetCartSummary;

public class GetCartSummaryHandler(
    ICartQueryService cartQueryService) : IRequestHandler<GetCartSummaryQuery, ServiceResult<CartSummaryDto>>
{
    public async Task<ServiceResult<CartSummaryDto>> Handle(
        GetCartSummaryQuery request,
        CancellationToken ct)
    {
        UserId? userId = request.UserId.HasValue ? UserId.From(request.UserId.Value) : null;
        GuestToken? guestToken = GuestToken.TryCreate(request.GuestToken);

        if (userId is null && guestToken is null)
            return ServiceResult<CartSummaryDto>.Validation("UserId یا GuestToken الزامی است.");

        var summary = await cartQueryService.GetCartSummaryAsync(userId, guestToken, ct);

        return ServiceResult<CartSummaryDto>.Success(summary);
    }
}