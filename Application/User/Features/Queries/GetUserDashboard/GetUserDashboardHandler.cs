using Application.Common.Results;
using Application.User.Contracts;

namespace Application.User.Features.Queries.GetUserDashboard;

public class GetUserDashboardHandler(IUserQueryService userQueryService)
        : IRequestHandler<GetUserDashboardQuery, ServiceResult<UserDashboardDto>>
{
    private readonly IUserQueryService _userQueryService = userQueryService;

    public async Task<ServiceResult<UserDashboardDto>> Handle(
        GetUserDashboardQuery request,
        CancellationToken ct)
    {
        var dashboard = await _userQueryService.GetUserDashboardAsync(
            request.UserId, ct);

        if (dashboard is null)
            return ServiceResult<UserDashboardDto>.NotFound("کاربر یافت نشد.");

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