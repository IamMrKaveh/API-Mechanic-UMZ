namespace MainApi.Audit.Controllers;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = "Admin")]
[Tags("Admin - Audit Logs")]
public sealed class AdminAuditLogsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(GetAuditLogsResult), 200)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int? userId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] string? action = null,
        [FromQuery] string? keyword = null,
        [FromQuery] string? ipAddress = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string sortBy = "Timestamp",
        [FromQuery] bool sortDesc = true,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAuditLogsQuery(
            UserId: userId,
            EventType: eventType,
            Action: action,
            Keyword: keyword,
            IpAddress: ipAddress,
            From: from,
            To: to,
            Page: page,
            PageSize: pageSize,
            SortBy: sortBy,
            SortDesc: sortDesc),
            ct);

        return Ok(result);
    }

    [HttpGet("statistics")]
    [ProducesResponseType(typeof(AuditStatisticsDto), 200)]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAuditStatisticsQuery(from, to), ct);
        return Ok(result);
    }

    [HttpGet("export/csv")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] int? userId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int maxRows = 10_000,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ExportAuditLogsQuery(
            UserId: userId,
            EventType: eventType,
            From: from,
            To: to,
            Format: "csv",
            MaxRows: maxRows),
            ct);

        return File(result.FileContent, result.ContentType, result.FileName);
    }

    [HttpGet("export/json")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ExportJson(
        [FromQuery] int? userId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int maxRows = 5_000,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ExportAuditLogsQuery(
            UserId: userId,
            EventType: eventType,
            From: from,
            To: to,
            Format: "json",
            MaxRows: maxRows),
            ct);

        return File(result.FileContent, result.ContentType, result.FileName);
    }

    [HttpGet("event-types")]
    [ProducesResponseType(typeof(IEnumerable<string>), 200)]
    public IActionResult GetEventTypes()
    {
        var types = new[]
        {
            "UserAction", "SecurityEvent", "AdminEvent",
            "OrderEvent", "PaymentEvent", "InventoryEvent",
            "CartEvent", "ProductEvent", "SystemEvent",
            "AuthEvent", "RefundEvent"
        };
        return Ok(types);
    }
}