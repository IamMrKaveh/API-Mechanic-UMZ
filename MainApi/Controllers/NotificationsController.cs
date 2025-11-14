using Application.Common.Interfaces;
using MainApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public NotificationsController(INotificationService notificationService, ICurrentUserService currentUserService)
    {
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Domain.Notification.Notification>>> GetMyNotifications([FromQuery] bool unreadOnly = false)
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var notifications = await _notificationService.GetUserNotificationsAsync(userId.Value, unreadOnly);
        return Ok(notifications);
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var count = await _notificationService.GetUnreadCountAsync(userId.Value);
        return Ok(count);
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var success = await _notificationService.MarkAsReadAsync(id, userId.Value);
        if (!success) return NotFound("Notification not found or already read.");

        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        await _notificationService.MarkAllAsReadAsync(userId.Value);
        return NoContent();
    }
}