using Application.Common.Results;
using Domain.Cart.Aggregates;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.Common.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Cart.Features.Commands.AddToCart;

public class AddToCartHandler(
    ICartRepository cartRepository,
    IVariantRepository variantRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<AddToCartCommand, ServiceResult>
{
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly IVariantRepository _variantRepository = variantRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(AddToCartCommand request, CancellationToken ct)
    {
        var variant = await _variantRepository.GetByIdAsync(request.VariantId, ct);
        if (variant is null || variant.IsDeleted || !variant.IsActive)
            return ServiceResult.NotFound("محصول یافت نشد یا فعال نیست.");

        if (!variant.IsUnlimited && variant.AvailableStock < request.Quantity)
            return ServiceResult.Failure("موجودی کافی نیست.");

        Domain.Cart.Aggregates.Cart? cart;
        if (request.UserId.HasValue)
        {
            cart = await _cartRepository.FindByUserIdAsync(request.UserId.Value, ct);
            if (cart is null)
            {
                cart = Domain.Cart.Aggregates.Cart.CreateForUser(request.UserId.Value);
                _cartRepository.Add(cart);
            }
        }
        else
        {
            var guestToken = GuestToken.Create(request.GuestToken!);
            cart = await _cartRepository.FindByGuestTokenAsync(guestToken, ct);
            if (cart is null)
            {
                cart = Domain.Cart.Aggregates.Cart.CreateForGuest(guestToken);
                _cartRepository.Add(cart);
            }
        }

        cart.AddItem(
            variant.Id.Value,
            variant.ProductId.Value,
            variant.Product?.Name.Value ?? "نامشخص",
            variant.Sku.Value,
            variant.SellingPrice,
            variant.OriginalPrice,
            request.Quantity);

        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}