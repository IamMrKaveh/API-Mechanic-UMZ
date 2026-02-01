namespace Application.Features.Cart.Queries.GetCartItemsCount;

public record GetCartItemsCountQuery(int? UserId, string? GuestId) : IRequest<int>;

public class GetCartItemsCountQueryHandler : IRequestHandler<GetCartItemsCountQuery, int>
{
    private readonly ICartService _cartService;


    public GetCartItemsCountQueryHandler(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<int> Handle(GetCartItemsCountQuery request, CancellationToken cancellationToken)
    {
        return await _cartService.GetCartItemsCountAsync(request.UserId, request.GuestId);
    }
}