using Application.Audit.Features.Queries.ExportAuditLogs;
using Application.Audit.Features.Queries.GetAuditLogs;
using Application.Audit.Features.Queries.GetAuditStatistics;

namespace Presentation.Audit.Endpoints;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = "Admin")]
[Tags("Admin - Audit Logs")]
public sealed class AdminAuditLogsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] Guid? userId = null,
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
        var query = new GetAuditLogsQuery(userId, eventType, action, keyword, ipAddress, from, to, page, pageSize, sortBy, sortDesc);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var query = new GetAuditStatisticsQuery(from, to);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] Guid? userId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int maxRows = 10_000,
        CancellationToken ct = default)
    {
        var query = new ExportAuditLogsQuery(userId, eventType, from, to, "csv", maxRows);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("export/json")]
    public async Task<IActionResult> ExportJson(
        [FromQuery] Guid? userId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int maxRows = 5_000,
        CancellationToken ct = default)
    {
        var query = new ExportAuditLogsQuery(userId, eventType, from, to, "json", maxRows);
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }
}