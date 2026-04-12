using Application.Audit.Features.Queries.ExportAuditLogs;
using Application.Audit.Features.Queries.GetAuditLogs;
using Application.Audit.Features.Queries.GetAuditStatistics;
using MapsterMapper;
using Presentation.Audit.Requests;

namespace Presentation.Audit.Endpoints;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = "Admin")]
[Tags("Admin - Audit Logs")]
public sealed class AdminAuditLogsController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] GetAuditLogsRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetAuditLogsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] GetAuditStatisticsRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetAuditStatisticsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] ExportAuditLogsRequest request,
        CancellationToken ct)
    {
        var query = new ExportAuditLogsQuery(
            request.UserId,
            request.EventType,
            request.EntityType,
            request.From,
            request.To,
            "csv",
            request.MaxRows ?? 10_000);

        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("export/json")]
    public async Task<IActionResult> ExportJson(
        [FromQuery] ExportAuditLogsRequest request,
        CancellationToken ct)
    {
        var query = new ExportAuditLogsQuery(
            request.UserId,
            request.EventType,
            request.EntityType,
            request.From,
            request.To,
            "json",
            request.MaxRows ?? 5_000);

        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}