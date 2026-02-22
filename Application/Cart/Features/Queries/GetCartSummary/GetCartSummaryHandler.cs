namespace Application.Cart.Features.Queries.GetCartSummary;

public class GetCartSummaryHandler : IRequestHandler<GetCartSummaryQuery, ServiceResult<CartSummaryDto>>
{
    private readonly ICartQueryService _cartQueryService;
    private readonly ICurrentUserService _currentUser;

    public GetCartSummaryHandler(
        ICartQueryService cartQueryService,
        ICurrentUserService currentUser)
    {
        _cartQueryService = cartQueryService;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<CartSummaryDto>> Handle(
        GetCartSummaryQuery request, CancellationToken cancellationToken)
    {
        var summary = await _cartQueryService.GetCartSummaryAsync(
            _currentUser.UserId, _currentUser.GuestId, cancellationToken);

        return ServiceResult<CartSummaryDto>.Success(summary);
    }
}