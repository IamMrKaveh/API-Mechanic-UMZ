namespace Application.Services;

public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaService _mediaService;

    public WishlistService(IWishlistRepository repository, IUnitOfWork unitOfWork, IMediaService mediaService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mediaService = mediaService;
    }

    public async Task<ServiceResult<List<WishlistDto>>> GetUserWishlistAsync(int userId)
    {
        var items = await _repository.GetByUserIdAsync(userId);
        var dtos = new List<WishlistDto>();

        foreach (var item in items)
        {
            var imageUrl = await _mediaService.GetPrimaryImageUrlAsync("Product", item.ProductId);
            dtos.Add(new WishlistDto(
                item.Id,
                item.ProductId,
                item.Product.Name,
                imageUrl ?? string.Empty,
                item.Product.MinPrice,
                item.Product.TotalStock > 0
            ));
        }

        return ServiceResult<List<WishlistDto>>.Ok(dtos);
    }

    public async Task<ServiceResult> ToggleWishlistAsync(int userId, int productId)
    {
        var existing = await _repository.GetByProductAsync(userId, productId);
        if (existing != null)
        {
            _repository.Remove(existing);
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        var wishlist = new Wishlist
        {
            UserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(wishlist);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<bool>> IsInWishlistAsync(int userId, int productId)
    {
        var exists = await _repository.ExistsAsync(userId, productId);
        return ServiceResult<bool>.Ok(exists);
    }
}