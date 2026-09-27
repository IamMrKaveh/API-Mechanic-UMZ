using Application.Cart.Features.Shared;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.Inventory.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Cart.Features.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityHandler(
    ICartRepository cartRepository,
    IVariantRepository variantRepository,
    IInventoryRepository inventoryRepository,
    ICartQueryService cartQueryService,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateCartItemQuantityCommand, CartDetailDto>
{
    public async Task<ServiceResult<CartDetailDto>> Handle(
        UpdateCartItemQuantityCommand request,
        CancellationToken ct)
    {
        UserId? userId = currentUserService.UserId.HasValue ? UserId.From(currentUserService.UserId.Value) : null;
        GuestToken? guestToken = GuestToken.TryCreate(currentUserService.GuestToken);

        if (userId is null && guestToken is null)
            return ServiceResult<CartDetailDto>.Validation("UserId یا GuestToken الزامی است.");

        var variantId = VariantId.From(request.VariantId);

        var variantResult = await variantRepository.GetByIdAsync(variantId, ct).OrNotFoundAsync(v => v.IsDeleted, "محصول یافت نشد.");
        if (variantResult.IsFailure) return variantResult.Error;
        var variant = variantResult.Value;

        var inventoryResult = await (inventoryRepository.GetByVariantIdAsync(variantId, ct)).OrNotFoundAsync("اطلاعات موجودی یافت نشد.");
        if (inventoryResult.IsFailure) return inventoryResult.Error;
        var inventory = inventoryResult.Value;

        if (!inventory.CanFulfill(request.Quantity))
            return ServiceResult<CartDetailDto>.Validation($"موجودی کافی نیست. موجود: {inventory.AvailableQuantity}");

        var cartResult = await (userId is not null
            ? cartRepository.FindByUserIdAsync(userId, ct)
            : cartRepository.FindByGuestTokenAsync(guestToken!, ct)).OrNotFoundAsync("سبد خرید یافت نشد.");
        if (cartResult.IsFailure) return cartResult.Error;
        var cart = cartResult.Value;

        cart.UpdateItemQuantity(variantId, request.Quantity, dateTimeProvider.UtcNow);
        cartRepository.Update(cart);
        await unitOfWork.SaveChangesAsync(ct);

        var cartDetail = await cartQueryService.GetCartDetailAsync(userId, guestToken, ct);

        return ServiceResult<CartDetailDto>.Success(cartDetail!);
    }
}