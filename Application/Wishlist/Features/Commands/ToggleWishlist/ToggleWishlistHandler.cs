using Application.Common.Results;
using Application.Wishlist.Contracts;
using Domain.Common.Interfaces;
using Domain.Wishlist.Interfaces;

namespace Application.Wishlist.Features.Commands.ToggleWishlist;

public class ToggleWishlistHandler(
    IWishlistRepository wishlistRepository,
    IWishlistQueryService wishlistQueryService,
    IUnitOfWork unitOfWork,
    ILogger<ToggleWishlistHandler> logger) : IRequestHandler<ToggleWishlistCommand, ServiceResult<bool>>
{
    private readonly IWishlistRepository _wishlistRepository = wishlistRepository;
    private readonly IWishlistQueryService _wishlistQueryService = wishlistQueryService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<ToggleWishlistHandler> _logger = logger;

    public async Task<ServiceResult<bool>> Handle(
        ToggleWishlistCommand request,
        CancellationToken ct)
    {
        try
        {
            var isInWishlist = await _wishlistQueryService.IsInWishlistAsync(
                request.UserId, request.ProductId, ct);

            if (isInWishlist)
            {
                await _wishlistRepository.RemoveAsync(
                    request.UserId, request.ProductId, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "محصول {ProductId} از لیست علاقه‌مندی کاربر {UserId} حذف شد.",
                    request.ProductId, request.UserId);

                return ServiceResult<bool>.Success(false);
            }

            var wishlist = Domain.Wishlist.Aggregates.Wishlist.Create(request.UserId, request.ProductId);
            await _wishlistRepository.AddAsync(wishlist, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "محصول {ProductId} به لیست علاقه‌مندی کاربر {UserId} اضافه شد.",
                request.ProductId, request.UserId);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "خطا در تغییر وضعیت علاقه‌مندی محصول {ProductId} برای کاربر {UserId}",
                request.ProductId, request.UserId);
            return ServiceResult<bool>.Unexpected("خطای داخلی سرور.");
        }
    }
}