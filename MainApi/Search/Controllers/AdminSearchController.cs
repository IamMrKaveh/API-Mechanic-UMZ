namespace MainApi.Search.Controllers;

[ApiController]
[Route("api/admin/search")]
public class AdminSearchController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminSearchController(IMediator mediator, ICurrentUserService currentUserService) : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncAllData(CancellationToken ct = default)
    {
        var command = new SyncSearchDataCommand();
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("recreate-indices")]
    public async Task<IActionResult> RecreateIndices(CancellationToken ct = default)
    {
        var command = new RecreateSearchIndicesCommand();
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetIndexStats(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSearchIndexStatsQuery(), ct);
        return ToActionResult(result);
    }
}