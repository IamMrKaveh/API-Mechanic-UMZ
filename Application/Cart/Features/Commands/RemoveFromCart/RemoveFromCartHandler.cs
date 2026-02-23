namespace Application.Cart.Features.Commands.RemoveFromCart;

public class RemoveFromCartHandler : IRequestHandler<RemoveFromCartCommand, ServiceResult<CartDetailDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartQueryService _cartQueryService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveFromCartHandler(
        ICartRepository cartRepository,
        ICartQueryService cartQueryService,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork
        )
    {
        _cartRepository = cartRepository;
        _cartQueryService = cartQueryService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<CartDetailDto>> Handle(
        RemoveFromCartCommand request,
        CancellationToken ct
        )
    {
        var cart = await _cartRepository.GetCartAsync(
            _currentUser.UserId, _currentUser.GuestId, ct);
        if (cart == null)
            return ServiceResult<CartDetailDto>.Failure("سبد خرید یافت نشد.", 404);

        cart.RemoveItem(request.VariantId);
        await _unitOfWork.SaveChangesAsync(ct);

        var cartDetail = await _cartQueryService.GetCartDetailAsync(
            _currentUser.UserId, _currentUser.GuestId, ct);

        return ServiceResult<CartDetailDto>.Success(cartDetail!);
    }
}