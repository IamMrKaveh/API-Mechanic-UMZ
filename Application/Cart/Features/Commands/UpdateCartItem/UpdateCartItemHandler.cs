using Application.Cart.Contracts;
using Application.Cart.Features.Shared;
using Application.Common.Results;
using Domain.Cart.Interfaces;
using Domain.Common.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;

namespace Application.Cart.Features.Commands.UpdateCartItem;

public class UpdateCartItemHandler(
    ICartRepository cartRepository,
    IVariantRepository variantRepository,
    ICartQueryService cartQueryService,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateCartItemCommand, ServiceResult<CartDetailDto>>
{
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly IVariantRepository _variantRepository = variantRepository;
    private readonly ICartQueryService _cartQueryService = cartQueryService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult<CartDetailDto>> Handle(
        UpdateCartItemCommand request,
        CancellationToken ct)
    {
        if (request.Quantity <= 0)
            return ServiceResult<CartDetailDto>.Validation("تعداد باید بزرگتر از صفر باشد.");

        var variant = await _variantRepository.GetByIdAsync(request.VariantId, ct);
        if (variant is null || variant.IsDeleted)
            return ServiceResult<CartDetailDto>.NotFound("محصول یافت نشد.");

        if (!variant.IsUnlimited && variant.AvailableStock < request.Quantity)
            return ServiceResult<CartDetailDto>.Validation($"موجودی کافی نیست. موجود: {variant.AvailableStock}");

        Domain.Cart.Aggregates.Cart? cart;
        if (request.UserId.HasValue)
            cart = await _cartRepository.GetByUserIdAsync(UserId.From(request.UserId.Value), ct);
        else
            cart = await _cartRepository.GetByGuestTokenAsync(request.GuestToken!, ct);

        if (cart is null)
            return ServiceResult<CartDetailDto>.NotFound("سبد خرید یافت نشد.");

        cart.UpdateItemQuantity(request.VariantId, request.Quantity);
        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(ct);

        UserId? userId = request.UserId.HasValue ? UserId.From(request.UserId.Value) : null;
        var cartDetail = await _cartQueryService.GetCartDetailAsync(userId, request.GuestToken, ct);

        return ServiceResult<CartDetailDto>.Success(cartDetail!);
    }
}