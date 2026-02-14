namespace Application.User.Features.Queries.CheckWishlistStatus;

public class CheckWishlistStatusHandler : IRequestHandler<CheckWishlistStatusQuery, ServiceResult<bool>>
{
    private readonly IUserRepository _userRepository;

    public CheckWishlistStatusHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ServiceResult<bool>> Handle(
        CheckWishlistStatusQuery request, CancellationToken cancellationToken)
    {
        var isInWishlist = await _userRepository.IsInWishlistAsync(
            request.UserId, request.ProductId, cancellationToken);

        return ServiceResult<bool>.Success(isInWishlist);
    }
}