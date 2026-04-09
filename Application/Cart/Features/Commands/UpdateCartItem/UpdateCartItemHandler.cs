using Application.Cart.Contracts;
using Application.Cart.Features.Shared;
using Application.Common.Results;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.Common.Interfaces;
using Domain.Inventory.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Cart.Features.Commands.UpdateCartItem;

public class UpdateCartItemHandler(
    ICartRepository cartRepository,
    IVariantRepository variantRepository,
    IInventoryRepository inventoryRepository,
    ICartQueryService cartQueryService,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateCartItemCommand, ServiceResult<CartDetailDto>>
{
    public async Task<ServiceResult<CartDetailDto>> Handle(
        UpdateCartItemCommand request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);

        var variant = await variantRepository.GetByIdAsync(variantId, ct);
        if (variant is null || variant.IsDeleted)
            return ServiceResult<CartDetailDto>.NotFound("محصول یافت نشد.");

        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
            return ServiceResult<CartDetailDto>.NotFound("اطلاعات موجودی یافت نشد.");

        if (!inventory.CanFulfill(request.Quantity))
            return ServiceResult<CartDetailDto>.Validation($"موجودی کافی نیست. موجود: {inventory.AvailableQuantity}");

        Domain.Cart.Aggregates.Cart? cart;
        if (request.UserId.HasValue)
            cart = await cartRepository.FindByUserIdAsync(UserId.From(request.UserId.Value), ct);
        else
            cart = await cartRepository.FindByGuestTokenAsync(GuestToken.Create(request.GuestToken!), ct);

        if (cart is null)
            return ServiceResult<CartDetailDto>.NotFound("سبد خرید یافت نشد.");

        cart.UpdateItemQuantity(variantId, request.Quantity);
        cartRepository.Update(cart);
        await unitOfWork.SaveChangesAsync(ct);

        UserId? userId = request.UserId.HasValue ? UserId.From(request.UserId.Value) : null;
        var cartDetail = await cartQueryService.GetCartDetailAsync(userId, request.GuestToken, ct);

        return ServiceResult<CartDetailDto>.Success(cartDetail!);
    }
}