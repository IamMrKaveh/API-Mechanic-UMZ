using Application.Cart.Features.Shared;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
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
        UserId? userId = request.UserId.HasValue ? UserId.From(request.UserId.Value) : null;
        GuestToken? guestToken = GuestToken.TryCreate(request.GuestToken);

        if (userId is null && guestToken is null)
            return ServiceResult<CartDetailDto>.Validation("UserId یا GuestToken الزامی است.");

        var variantId = VariantId.From(request.VariantId);

        var variant = await variantRepository.GetByIdAsync(variantId, ct);
        if (variant is null || variant.IsDeleted)
            return ServiceResult<CartDetailDto>.NotFound("محصول یافت نشد.");

        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
            return ServiceResult<CartDetailDto>.NotFound("اطلاعات موجودی یافت نشد.");

        if (!inventory.CanFulfill(request.Quantity))
            return ServiceResult<CartDetailDto>.Validation($"موجودی کافی نیست. موجود: {inventory.AvailableQuantity}");

        Domain.Cart.Aggregates.Cart? cart = userId is not null
            ? await cartRepository.FindByUserIdAsync(userId, ct)
            : await cartRepository.FindByGuestTokenAsync(guestToken!, ct);

        if (cart is null)
            return ServiceResult<CartDetailDto>.NotFound("سبد خرید یافت نشد.");

        cart.UpdateItemQuantity(variantId, request.Quantity);
        cartRepository.Update(cart);
        await unitOfWork.SaveChangesAsync(ct);

        var cartDetail = await cartQueryService.GetCartDetailAsync(userId, guestToken, ct);

        return ServiceResult<CartDetailDto>.Success(cartDetail!);
    }
}