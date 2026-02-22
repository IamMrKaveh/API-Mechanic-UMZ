namespace Application.Cart.Features.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityHandler : IRequestHandler<UpdateCartItemQuantityCommand, ServiceResult<CartDetailDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICartQueryService _cartQueryService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateCartItemQuantityHandler> _logger;

    public UpdateCartItemQuantityHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        ICartQueryService cartQueryService,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        ILogger<UpdateCartItemQuantityHandler> logger)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _cartQueryService = cartQueryService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<CartDetailDto>> Handle(
        UpdateCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetCartAsync(
            _currentUser.UserId, _currentUser.GuestId, cancellationToken);
        if (cart == null)
            return ServiceResult<CartDetailDto>.Failure("سبد خرید یافت نشد.", 404);

        // اگر تعداد صفر باشد، حذف آیتم
        if (request.Quantity == 0)
        {
            cart.RemoveItem(request.VariantId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var updatedCart = await _cartQueryService.GetCartDetailAsync(
                _currentUser.UserId, _currentUser.GuestId, cancellationToken);
            return ServiceResult<CartDetailDto>.Success(updatedCart!);
        }

        // بررسی موجودی
        var variant = await _productRepository.GetVariantByIdAsync(request.VariantId, cancellationToken);
        if (variant == null || !variant.IsActive || variant.IsDeleted)
            return ServiceResult<CartDetailDto>.Failure("محصول یافت نشد یا غیرفعال است.", 404);

        if (!variant.IsUnlimited && variant.StockQuantity < request.Quantity)
            return ServiceResult<CartDetailDto>.Failure(
                $"موجودی کافی نیست. موجودی فعلی: {variant.StockQuantity}", 400);

        // به‌روزرسانی تعداد در Domain
        cart.UpdateItemQuantity(request.VariantId, request.Quantity);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "تعداد آیتم {VariantId} در سبد {CartId} به {Quantity} تغییر یافت.",
            request.VariantId, cart.Id, request.Quantity);

        var cartDetail = await _cartQueryService.GetCartDetailAsync(
            _currentUser.UserId, _currentUser.GuestId, cancellationToken);

        return ServiceResult<CartDetailDto>.Success(cartDetail!);
    }
}