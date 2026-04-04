using Application.Cart.Contracts;
using Application.Cart.Features.Shared;
using Application.Common.Results;
using Domain.Cart.Interfaces;
using Domain.Common.Interfaces;
using Domain.Product.Interfaces;
using SharedKernel.Contracts;

namespace Application.Cart.Features.Commands.UpdateCartItemQuantity;

/// <summary>
/// آپدیت موجودی یک آیتم در سبد خرید با استفاده از Domain Service برای اعتبارسنجی و اعمال قوانین تجاری
/// </summary>
public class UpdateCartItemQuantityHandler(
    ICartRepository cartRepository,
    IProductRepository productRepository,
    ICartQueryService cartQueryService,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork,
    CartItemValidationService cartItemValidationService,
    ILogger<UpdateCartItemQuantityHandler> logger) : IRequestHandler<UpdateCartItemQuantityCommand, ServiceResult<CartDetailDto>>
{
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly IProductRepository _productRepository = productRepository;
    private readonly ICartQueryService _cartQueryService = cartQueryService;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly CartItemValidationService _cartItemValidationService = cartItemValidationService;
    private readonly ILogger<UpdateCartItemQuantityHandler> _logger = logger;

    public async Task<ServiceResult<CartDetailDto>> Handle(
        UpdateCartItemQuantityCommand request,
        CancellationToken ct)
    {
        var cart = await _cartRepository.GetCartAsync(
            _currentUser.CurrentUser.UserId,
            _currentUser.GuestId,
            ct);
        if (cart == null)
            return ServiceResult<CartDetailDto>.NotFound("سبد خرید یافت نشد.");

        if (request.Quantity == 0)
        {
            cart.RemoveItem(request.VariantId);
            await _unitOfWork.SaveChangesAsync(ct);

            var updatedCart = await _cartQueryService.GetCartDetailAsync(
                _currentUser.CurrentUser.UserId, _currentUser.GuestId, ct);
            return ServiceResult<CartDetailDto>.Success(updatedCart!);
        }

        var variant = await _productRepository.GetVariantByIdAsync(request.VariantId, ct);
        if (variant == null)
            return ServiceResult<CartDetailDto>.NotFound("محصول یافت نشد.");
        if (!variant.IsActive || variant.IsDeleted)
            return ServiceResult<CartDetailDto>.Forbidden("محصول حذف شده یا غیر فعال هست.");

        var (isValid, error) = _cartItemValidationService.ValidateUpdateQuantity(
            request.Quantity,
            variant.AvailableStock,
            variant.IsUnlimited);

        if (!isValid)
            return ServiceResult<CartDetailDto>.Validation(error!);

        cart.UpdateItemQuantity(request.VariantId, request.Quantity);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "تعداد آیتم {VariantId} در سبد {CartId} به {Quantity} تغییر یافت.",
            request.VariantId, cart.Id, request.Quantity);

        var cartDetail = await _cartQueryService.GetCartDetailAsync(
            _currentUser.CurrentUser.UserId ?? null, _currentUser.GuestId, ct);

        return ServiceResult<CartDetailDto>.Success(cartDetail!);
    }
}