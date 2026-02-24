namespace Application.User.Features.Queries.GetUserDashboard;

public record GetUserDashboardQuery(int UserId) : IRequest<ServiceResult<UserDashboardDto>>;

public class UserDashboardDto
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int ActiveAddresses { get; set; }
    public int WishlistCount { get; set; }
    public int UnreadNotifications { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime MemberSince { get; set; }
}