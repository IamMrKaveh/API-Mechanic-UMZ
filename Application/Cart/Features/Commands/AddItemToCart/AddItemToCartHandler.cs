using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.Inventory.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Cart.Features.Commands.AddItemToCart;

public class AddItemToCartHandler(
    ICartRepository cartRepository,
    IVariantRepository variantRepository,
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : ICommandHandler<AddItemToCartCommand>
{
    public async Task<ServiceResult> Handle(AddItemToCartCommand request, CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var variant = await variantRepository.GetWithProductAsync(variantId, ct);

        if (variant is null)
            return ServiceResult.NotFound("واریانت یافت نشد.");

        if (!variant.IsActive)
            return ServiceResult.Failure("واریانت غیرفعال است.");

        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null || !inventory.CanFulfill(request.Quantity))
            return ServiceResult.Failure("موجودی کافی نیست.");

        Domain.Cart.Aggregates.Cart? cart;
        var isNewCart = false;

        if (currentUserService.UserId.HasValue)
        {
            var userId = UserId.From(currentUserService.UserId.Value);
            cart = await cartRepository.FindByUserIdAsync(userId, ct);

            if (cart is null)
            {
                cart = Domain.Cart.Aggregates.Cart.CreateForUser(userId);
                cartRepository.Add(cart);
                isNewCart = true;
            }
        }
        else if (!string.IsNullOrWhiteSpace(currentUserService.GuestToken))
        {
            var guestToken = GuestToken.Create(currentUserService.GuestToken);
            cart = await cartRepository.FindByGuestTokenAsync(guestToken, ct);

            if (cart is null)
            {
                cart = Domain.Cart.Aggregates.Cart.CreateForGuest(guestToken);
                cartRepository.Add(cart);
                isNewCart = true;
            }
        }
        else
        {
            return ServiceResult.Failure("کاربر یا توکن مهمان الزامی است.");
        }

        var productName = ProductName.Create(variant.Sku.Value);
        cart.AddItem(variantId, variant.ProductId, productName, variant.Sku, variant.SellingPrice, variant.OriginalPrice, request.Quantity);

        if (!isNewCart)
            cartRepository.Update(cart);

        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}