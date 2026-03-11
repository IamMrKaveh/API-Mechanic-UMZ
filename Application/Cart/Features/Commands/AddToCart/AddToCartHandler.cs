namespace Application.Cart.Features.Commands.AddToCart;

public sealed class AddToCartHandler(
    ICartRepository cartRepository,
    IProductVariantRepository variantRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService) : IRequestHandler<AddToCartCommand, ServiceResult<int>>
{
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly IProductVariantRepository _variantRepository = variantRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult<int>> Handle(
        AddToCartCommand request,
        CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;

        var variant = await _variantRepository.GetByIdAsync(request.VariantId, ct);
        if (variant is null)
            return ServiceResult<int>.Failure("Product variant not found.");

        var cart = await _cartRepository.GetByUserIdAsync(userId, ct)
                   ?? Cart.Create(userId);

        var cartItem = cart.AddItem(request.VariantId, request.Quantity, variant.Price);

        await _cartRepository.UpsertAsync(cart, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<int>.Success(cartItem.Id);
    }
}