namespace Application.User.Features.Queries.GetUserDashboard;

public class GetUserDashboardHandler
    : IRequestHandler<GetUserDashboardQuery, ServiceResult<UserDashboardDto>>
{
    private readonly IUserQueryService _userQueryService;

    public GetUserDashboardHandler(IUserQueryService userQueryService)
    {
        _userQueryService = userQueryService;
    }

    public async Task<ServiceResult<UserDashboardDto>> Handle(
        GetUserDashboardQuery request, CancellationToken cancellationToken)
    {
        var dashboard = await _userQueryService.GetUserDashboardAsync(
            request.UserId, cancellationToken);

        if (dashboard == null)
            return ServiceResult<UserDashboardDto>.Failure("کاربر یافت نشد.", 404);

        // نگاشت از UserDashboardDto (Shared) به UserDashboardDto (Query)
        var result = new UserDashboardDto
        {
            TotalOrders = dashboard.TotalOrders,
            TotalSpent = dashboard.TotalSpent,
            WishlistCount = dashboard.WishlistCount,
            UnreadNotifications = dashboard.UnreadNotifications,
            MemberSince = dashboard.UserProfile.CreatedAt,
            LastLoginAt = dashboard.UserProfile.LastLoginAt,
            ActiveAddresses = dashboard.UserProfile.UserAddresses.Count
        };

        return ServiceResult<UserDashboardDto>.Success(result);
    }
}