namespace Application.Features.Cart.Queries.GetCart;

public record GetCartQuery(int? UserId, string? GuestId) : IRequest<ServiceResult<CartDto?>>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, ServiceResult<CartDto?>>
{
    private readonly ICartService _cartService;

    public GetCartQueryHandler(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<ServiceResult<CartDto?>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await _cartService.GetCartAsync(request.UserId, request.GuestId);
        return ServiceResult<CartDto?>.Ok(cart);
    }
}