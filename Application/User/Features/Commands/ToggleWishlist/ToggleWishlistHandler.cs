namespace Application.User.Features.Commands.ToggleWishlist;

public class ToggleWishlistHandler : IRequestHandler<ToggleWishlistCommand, ServiceResult<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ToggleWishlistHandler> _logger;

    public ToggleWishlistHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<ToggleWishlistHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<bool>> Handle(ToggleWishlistCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var isInWishlist = await _userRepository.IsInWishlistAsync(
                request.UserId, request.ProductId, cancellationToken);

            if (isInWishlist)
            {
                await _userRepository.RemoveFromWishlistAsync(
                    request.UserId, request.ProductId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "محصول {ProductId} از لیست علاقه‌مندی کاربر {UserId} حذف شد.",
                    request.ProductId, request.UserId);

                return ServiceResult<bool>.Success(false);
            }
            else
            {
                await _userRepository.AddToWishlistAsync(
                    request.UserId, request.ProductId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "محصول {ProductId} به لیست علاقه‌مندی کاربر {UserId} اضافه شد.",
                    request.ProductId, request.UserId);

                return ServiceResult<bool>.Success(true);
            }
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