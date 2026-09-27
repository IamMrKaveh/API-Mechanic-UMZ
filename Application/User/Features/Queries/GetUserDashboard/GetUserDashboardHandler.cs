using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Queries.GetUserDashboard;

public class GetUserDashboardHandler(
    IUserQueryService userQueryService,
    ICurrentUserService currentUserService)
        : IQueryHandler<GetUserDashboardQuery, UserDashboardDto>
{
    public async Task<ServiceResult<UserDashboardDto>> Handle(
        GetUserDashboardQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(currentUserService.UserId!.Value);

        var dashboardResult = await (userQueryService.GetUserDashboardAsync(userId, ct)).OrNotFoundAsync("کاربر یافت نشد.");
        if (dashboardResult.IsFailure) return dashboardResult.Error;
        var dashboard = dashboardResult.Value;

        var profile = dashboard.UserProfile;

        var result = new UserDashboardDto
        {
            UserProfile = profile,
            TotalOrders = dashboard.TotalOrders,
            TotalSpent = dashboard.TotalSpent,
            WishlistCount = dashboard.WishlistCount,
            UnreadNotifications = dashboard.UnreadNotifications,
            CompletedOrders = dashboard.CompletedOrders,
            OpenTickets = dashboard.OpenTickets,
            MemberSince = profile?.CreatedAt ?? default,
            LastLoginAt = profile?.LastLoginAt,
            ActiveAddresses = profile?.UserAddresses.Count ?? 0
        };

        return ServiceResult<UserDashboardDto>.Success(result);
    }
}
