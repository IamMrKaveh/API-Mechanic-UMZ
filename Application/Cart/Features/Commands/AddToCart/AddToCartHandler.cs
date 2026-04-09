using Application.Common.Results;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.Common.Interfaces;
using Domain.Inventory.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Cart.Features.Commands.AddToCart;

public class AddToCartHandler(
    ICartRepository cartRepository,
    IVariantRepository variantRepository,
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<AddToCartCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(AddToCartCommand request, CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);

        var variant = await variantRepository.GetWithProductAsync(variantId, ct);
        if (variant is null || variant.IsDeleted || !variant.IsActive)
            return ServiceResult.NotFound("محصول یافت نشد یا فعال نیست.");

        if (variant.Product is null || string.IsNullOrWhiteSpace(variant.Product.Name))
            return ServiceResult.NotFound("اطلاعات محصول ناقص است.");

        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
            return ServiceResult.NotFound("اطلاعات موجودی یافت نشد.");

        if (!inventory.CanFulfill(request.Quantity))
            return ServiceResult.Failure("موجودی کافی نیست.", SharedKernel.Results.ErrorType.Validation);

        var productName = ProductName.Create(variant.Product.Name);
        var unitPrice = variant.Price;
        var originalPrice = variant.CompareAtPrice ?? variant.Price;

        Domain.Cart.Aggregates.Cart? cart;
        if (request.UserId.HasValue)
        {
            var userId = UserId.From(request.UserId.Value);
            cart = await cartRepository.FindByUserIdAsync(userId, ct);
            if (cart is null)
            {
                cart = Domain.Cart.Aggregates.Cart.CreateForUser(userId);
                cartRepository.Add(cart);
            }
        }
        else
        {
            var guestToken = GuestToken.Create(request.GuestToken!);
            cart = await cartRepository.FindByGuestTokenAsync(guestToken, ct);
            if (cart is null)
            {
                cart = Domain.Cart.Aggregates.Cart.CreateForGuest(guestToken);
                cartRepository.Add(cart);
            }
        }

        cart.AddItem(
            variant.Id,
            variant.ProductId,
            productName,
            variant.Sku,
            unitPrice,
            originalPrice,
            request.Quantity);

        cartRepository.Update(cart);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}