namespace Application.Cart.Features.Commands.AddToCart;

/// <summary>
/// اعتبارسنجی موجودی به Domain Service منتقل شده
/// Handler فقط orchestrate می‌کند و قوانین تجاری را به دامنه delegate می‌کند
/// </summary>
public class AddToCartHandler : IRequestHandler<AddToCartCommand, ServiceResult<CartDetailDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICartQueryService _cartQueryService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CartItemValidationService _cartItemValidationService;
    private readonly ILogger<AddToCartHandler> _logger;

    public AddToCartHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        ICartQueryService cartQueryService,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        CartItemValidationService cartItemValidationService,
        ILogger<AddToCartHandler> logger
        )
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _cartQueryService = cartQueryService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _cartItemValidationService = cartItemValidationService;
        _logger = logger;
    }

    public async Task<ServiceResult<CartDetailDto>> Handle(
        AddToCartCommand request,
        CancellationToken ct
        )
    {
        
        var variant = await _productRepository.GetVariantByIdAsync(request.VariantId, ct);
        if (variant == null || !variant.IsActive || variant.IsDeleted)
            return ServiceResult<CartDetailDto>.Failure("محصول یافت نشد یا غیرفعال است.", 404);

        
        var cart = await _cartRepository.GetCartAsync(_currentUser.UserId, _currentUser.GuestId, ct);

        if (cart == null)
        {
            cart = Domain.Cart.Cart.Create(_currentUser.UserId, _currentUser.GuestId);
            await _cartRepository.AddAsync(cart, ct);
        }

        
        var existingItem = cart.FindItemByVariant(request.VariantId);
        var currentCartQuantity = existingItem?.Quantity ?? 0;

        var (isValid, error) = _cartItemValidationService.ValidateAddToCart(
            request.Quantity,
            variant.AvailableStock,
            variant.IsUnlimited,
            currentCartQuantity);

        if (!isValid)
            return ServiceResult<CartDetailDto>.Failure(error!, 400);

        
        cart.AddItem(request.VariantId, request.Quantity, variant.SellingPrice);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "آیتم {VariantId} با تعداد {Quantity} به سبد {CartId} اضافه شد.",
            request.VariantId, request.Quantity, cart.Id);

        
        var cartDetail = await _cartQueryService.GetCartDetailAsync(
            _currentUser.UserId, _currentUser.GuestId, ct);

        return ServiceResult<CartDetailDto>.Success(cartDetail!);
    }
}