using Application.Common.Interfaces.Log;

namespace MainApi.Controllers.Admin;

[ApiController]
[Route("api/admin/AuditLogs")]
[Authorize(Roles = "Admin")]
public class AdminAuditLogsController : ControllerBase
{
    private readonly IAuditService _auditService;
    public AdminAuditLogsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int? userId,
        [FromQuery] string? eventType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (logs, totalItems) = await _auditService.GetAuditLogsAsync(userId, eventType, fromDate, toDate, page, pageSize);
        return Ok(new
        {
            Items = logs,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        });
    }
}