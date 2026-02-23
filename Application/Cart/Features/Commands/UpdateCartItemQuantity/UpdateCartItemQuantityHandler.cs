namespace Application.Cart.Features.Commands.UpdateCartItemQuantity;

/// <summary>
/// آپدیت موجودی یک آیتم در سبد خرید با استفاده از Domain Service برای اعتبارسنجی و اعمال قوانین تجاری
/// </summary>
public class UpdateCartItemQuantityHandler : IRequestHandler<UpdateCartItemQuantityCommand, ServiceResult<CartDetailDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICartQueryService _cartQueryService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CartItemValidationService _cartItemValidationService;
    private readonly ILogger<UpdateCartItemQuantityHandler> _logger;

    public UpdateCartItemQuantityHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        ICartQueryService cartQueryService,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        CartItemValidationService cartItemValidationService,
        ILogger<UpdateCartItemQuantityHandler> logger
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
        UpdateCartItemQuantityCommand request,
        CancellationToken ct
        )
    {
        var cart = await _cartRepository.GetCartAsync(
            _currentUser.UserId, _currentUser.GuestId, ct);
        if (cart == null)
            return ServiceResult<CartDetailDto>.Failure("سبد خرید یافت نشد.", 404);

        // اگر تعداد صفر باشد، حذف آیتم
        if (request.Quantity == 0)
        {
            cart.RemoveItem(request.VariantId);
            await _unitOfWork.SaveChangesAsync(ct);

            var updatedCart = await _cartQueryService.GetCartDetailAsync(
                _currentUser.UserId, _currentUser.GuestId, ct);
            return ServiceResult<CartDetailDto>.Success(updatedCart!);
        }

        // بارگذاری واریانت برای بررسی موجودی
        var variant = await _productRepository.GetVariantByIdAsync(request.VariantId, ct);
        if (variant == null || !variant.IsActive || variant.IsDeleted)
            return ServiceResult<CartDetailDto>.Failure("محصول یافت نشد یا غیرفعال است.", 404);

        // اعتبارسنجی از طریق Domain Service با استفاده از AvailableStock
        var (isValid, error) = _cartItemValidationService.ValidateUpdateQuantity(
            request.Quantity,
            variant.AvailableStock,
            variant.IsUnlimited);

        if (!isValid)
            return ServiceResult<CartDetailDto>.Failure(error!, 400);

        // به‌روزرسانی تعداد در Domain Aggregate
        cart.UpdateItemQuantity(request.VariantId, request.Quantity);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "تعداد آیتم {VariantId} در سبد {CartId} به {Quantity} تغییر یافت.",
            request.VariantId, cart.Id, request.Quantity);

        var cartDetail = await _cartQueryService.GetCartDetailAsync(
            _currentUser.UserId, _currentUser.GuestId, ct);

        return ServiceResult<CartDetailDto>.Success(cartDetail!);
    }
}