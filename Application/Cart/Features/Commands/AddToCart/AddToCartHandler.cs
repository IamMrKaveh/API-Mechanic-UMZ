namespace Application.Cart.Features.Commands.AddToCart;

public class AddToCartHandler : IRequestHandler<AddToCartCommand, ServiceResult<CartDetailDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICartQueryService _cartQueryService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddToCartHandler> _logger;

    public AddToCartHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        ICartQueryService cartQueryService,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        ILogger<AddToCartHandler> logger)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _cartQueryService = cartQueryService;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<CartDetailDto>> Handle(
        AddToCartCommand request, CancellationToken cancellationToken)
    {
        // ۱. بررسی موجودی در Application Layer
        var variant = await _productRepository.GetVariantByIdAsync(request.VariantId, cancellationToken);
        if (variant == null || !variant.IsActive || variant.IsDeleted)
            return ServiceResult<CartDetailDto>.Failure("محصول یافت نشد یا غیرفعال است.", 404);

        if (!variant.IsUnlimited && variant.StockQuantity < request.Quantity)
            return ServiceResult<CartDetailDto>.Failure(
                $"موجودی کافی نیست. موجودی فعلی: {variant.StockQuantity}", 400);

        // ۲. دریافت یا ایجاد سبد
        var cart = await _cartRepository.GetCartAsync(_currentUser.UserId, _currentUser.GuestId, cancellationToken);

        if (cart == null)
        {
            cart = Domain.Cart.Cart.Create(_currentUser.UserId, _currentUser.GuestId);
            await _cartRepository.AddAsync(cart, cancellationToken);
        }

        // ۳. بررسی مجموع تعداد (آیتم موجود + جدید) با موجودی
        var existingItem = cart.FindItemByVariant(request.VariantId);
        if (existingItem != null && !variant.IsUnlimited)
        {
            var totalQuantity = existingItem.Quantity + request.Quantity;
            if (totalQuantity > variant.StockQuantity)
                return ServiceResult<CartDetailDto>.Failure(
                    $"موجودی کافی نیست. موجودی فعلی: {variant.StockQuantity}، تعداد در سبد: {existingItem.Quantity}", 400);
        }

        // ۴. افزودن به سبد (Domain فقط ساختار را مدیریت می‌کند)
        cart.AddItem(request.VariantId, request.Quantity, variant.SellingPrice);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "آیتم {VariantId} با تعداد {Quantity} به سبد {CartId} اضافه شد.",
            request.VariantId, request.Quantity, cart.Id);

        // ۵. بازگرداندن DTO کامل از QueryService
        var cartDetail = await _cartQueryService.GetCartDetailAsync(
            _currentUser.UserId, _currentUser.GuestId, cancellationToken);

        return ServiceResult<CartDetailDto>.Success(cartDetail!);
    }
}