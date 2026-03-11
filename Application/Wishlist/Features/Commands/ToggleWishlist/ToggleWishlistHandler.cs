using Application.Common.Models;
using Domain.Wishlist.Interfaces;

namespace Application.Wishlist.Features.Commands.ToggleWishlist;

public class ToggleWishlistHandler : IRequestHandler<ToggleWishlistCommand, ServiceResult<bool>>
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IWishlistQueryService _wishlistQueryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ToggleWishlistHandler> _logger;

    public ToggleWishlistHandler(
        IWishlistRepository wishlistRepository,
        IWishlistQueryService wishlistQueryService,
        IUnitOfWork unitOfWork,
        ILogger<ToggleWishlistHandler> logger)
    {
        _wishlistRepository = wishlistRepository;
        _wishlistQueryService = wishlistQueryService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<bool>> Handle(
        ToggleWishlistCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var isInWishlist = await _wishlistQueryService.IsInWishlistAsync(
                request.UserId, request.ProductId, cancellationToken);

            if (isInWishlist)
            {
                await _wishlistRepository.RemoveAsync(
                    request.UserId, request.ProductId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "محصول {ProductId} از لیست علاقه‌مندی کاربر {UserId} حذف شد.",
                    request.ProductId, request.UserId);

                return ServiceResult<bool>.Success(false);
            }

            var wishlist = Domain.Wishlist.Aggregates.Wishlist.Create(request.UserId, request.ProductId);
            await _wishlistRepository.AddAsync(wishlist, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            return ServiceResult<bool>.Failure("خطای داخلی سرور.");
        }
    }
}