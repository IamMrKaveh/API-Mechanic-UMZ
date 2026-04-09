using Application.Cart.Contracts;
using Application.Cart.Features.Shared;
using SharedKernel.Contracts;

namespace Application.Cart.Features.Queries.GetCartSummary;

public class GetCartSummaryHandler(
    ICartQueryService cartQueryService,
    ICurrentUserService currentUser) : IRequestHandler<GetCartSummaryQuery, ServiceResult<CartSummaryDto>>
{
    private readonly ICartQueryService _cartQueryService = cartQueryService;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<ServiceResult<CartSummaryDto>> Handle(
        GetCartSummaryQuery request,
        CancellationToken ct)
    {
        var summary = await _cartQueryService.GetCartSummaryAsync(
            _currentUser.UserId, _currentUser.GuestId, ct);

        return ServiceResult<CartSummaryDto>.Success(summary);
    }
}