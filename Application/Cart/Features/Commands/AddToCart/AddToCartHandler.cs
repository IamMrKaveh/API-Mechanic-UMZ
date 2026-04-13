using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.Common.ValueObjects;
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
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<AddToCartCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(AddToCartCommand request, CancellationToken ct)
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
        else if (!string.IsNullOrWhiteSpace(request.GuestToken))
        {
            var guestToken = GuestToken.Create(request.GuestToken);
            cart = await cartRepository.FindByGuestTokenAsync(guestToken, ct);

            if (cart is null)
            {
                cart = Domain.Cart.Aggregates.Cart.CreateForGuest(guestToken);
                cartRepository.Add(cart);
            }
        }
        else
        {
            return ServiceResult.Failure("کاربر یا توکن مهمان الزامی است.");
        }

        var productName = ProductName.Create(variant.Sku.Value);
        cart.AddItem(variantId, variant.ProductId, productName, variant.Sku, variant.Price, variant.CompareAtPrice ?? variant.Price, request.Quantity);
        cartRepository.Update(cart);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}