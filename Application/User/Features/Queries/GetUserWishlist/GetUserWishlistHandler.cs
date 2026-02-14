namespace Application.User.Features.Queries.GetUserWishlist;

public class GetUserWishlistHandler
    : IRequestHandler<GetUserWishlistQuery, ServiceResult<PaginatedResult<WishlistItemDto>>>
{
    private readonly IUserQueryService _userQueryService;

    public GetUserWishlistHandler(IUserQueryService userQueryService)
    {
        _userQueryService = userQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<WishlistItemDto>>> Handle(
        GetUserWishlistQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _userQueryService.GetUserWishlistPagedAsync(
            request.UserId,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<WishlistItemDto>>.Success(result);
    }
}