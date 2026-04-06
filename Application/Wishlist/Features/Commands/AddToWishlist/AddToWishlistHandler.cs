using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Aggregates;
using Domain.Wishlist.Interfaces;

namespace Application.Wishlist.Features.Commands.AddToWishlist;

public class AddToWishlistHandler(
    IWishlistRepository wishlistRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ILogger<AddToWishlistHandler> logger) : IRequestHandler<AddToWishlistCommand, ServiceResult>
{
    private readonly IWishlistRepository _wishlistRepository = wishlistRepository;
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<AddToWishlistHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        AddToWishlistCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var productId = ProductId.From(request.ProductId);

        var product = await _productRepository.GetByIdAsync(productId, ct);
        if (product is null || !product.IsActive)
            return ServiceResult.NotFound("محصول یافت نشد.");

        if (await _wishlistRepository.ExistsAsync(userId, productId, ct))
            return ServiceResult.Conflict("این محصول قبلاً به علاقه‌مندی‌ها اضافه شده است.");

        var wishlistItem = Wishlist.Create(userId, productId);
        await _wishlistRepository.AddAsync(wishlistItem, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} added to wishlist for user {UserId}", request.ProductId, request.UserId);
        return ServiceResult.Success();
    }
}