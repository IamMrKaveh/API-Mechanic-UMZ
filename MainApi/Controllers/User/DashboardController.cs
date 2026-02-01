using Application.Common.Interfaces.Notification;
using Application.Common.Interfaces.Order;
using Application.Common.Interfaces.User;
using MainApi.Controllers.Base;

namespace MainApi.Controllers.User;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly IOrderService _orderService;
    private readonly IWishlistService _wishlistService;
    private readonly ITicketService _ticketService;
    private readonly INotificationService _notificationService;

    public DashboardController(
        IUserService userService,
        IOrderService orderService,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IWishlistService wishlistService,
        ITicketService ticketService) : base(currentUserService)
    {
        _userService = userService;
        _orderService = orderService;
        _notificationService = notificationService;
        _wishlistService = wishlistService;
        _ticketService = ticketService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<UserDashboardDto>> GetDashboardSummary()
    {
        var userId = CurrentUser.UserId;
        if (!userId.HasValue) return Unauthorized();

        var profileTask = _userService.GetUserProfileAsync(userId.Value);
        var ordersTask = _orderService.GetOrdersAsync(userId, false, userId, null, null, null, 1, 5);
        var activitiesTask = _userService.GetUserActivitiesAsync(userId.Value, 10);
        var notificationsTask = _notificationService.GetUnreadCountAsync(userId.Value);
        var wishlistTask = _wishlistService.GetUserWishlistAsync(userId.Value);
        var ticketsTask = _ticketService.GetUserTicketsAsync(userId.Value);

        await Task.WhenAll(profileTask, ordersTask, activitiesTask, notificationsTask, wishlistTask, ticketsTask);

        var profileResult = await profileTask;
        var (orders, totalOrders) = await ordersTask;
        var activitiesResult = await activitiesTask;
        var unreadNotifications = await notificationsTask;
        int wishlistCount = wishlistTask.Result.Data?.Count ?? 0;
        int openTicketsCount = ticketsTask.Result.Data?.Count(t => t.Status == "Open") ?? 0;

        if (!profileResult.Success)
            return ServiceResult.Fail(profileResult);

        var dashboardDto = new UserDashboardDto
        {
            UserProfile = profileResult.Data!,
            RecentOrders = orders.ToList(),
            RecentActivities = activitiesResult.Data ?? [],
            UnreadNotifications = unreadNotifications,
            TotalOrders = totalOrders,
            TotalSpent = orders.Sum(o => o.FinalAmount),
            WishlistCount = wishlistCount,
            OpenTicketsCount = openTicketsCount
        };

        return Ok(dashboardDto);
    }
}