using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Wishlist.Features.Commands.AddToWishlist;

public class AddToWishlistHandler(
    IWishlistRepository wishlistRepository,
    IProductRepository productRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AddToWishlistCommand>
{
    public async Task<ServiceResult> Handle(
        AddToWishlistCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var productId = ProductId.From(request.ProductId);

        var productResult = await productRepository.GetByIdAsync(productId, ct).OrNotFoundAsync(p => !p.IsActive, "محصول یافت نشد.");
        if (productResult.IsFailure) return productResult.ToServiceResult();

        if (await wishlistRepository.ExistsAsync(userId, productId, ct))
            return ServiceResult.Conflict("این محصول قبلاً به علاقه‌مندی‌ها اضافه شده است.");

        var wishlistItem = Domain.Wishlist.Aggregates.Wishlist.Create(userId, productId, dateTimeProvider.UtcNow);
        await wishlistRepository.AddAsync(wishlistItem, ct);

        return ServiceResult.Success();
    }
}