namespace MainApi.Search.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
public class AdminSearchController : BaseApiController
{
    // نکته: این کنترلر از IMediator استفاده می‌کند اما در کد اصلی سرویس‌ها مستقیم اینجکت شده بودند.
    // اینجا از Mediator استفاده می‌کنیم و فرض بر این است که کامندها وجود دارند یا ساخته خواهند شد.
    private readonly IMediator _mediator;

    public AdminSearchController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncAllData(CancellationToken ct = default)
    {
        // نیاز به پیاده‌سازی SyncSearchDataCommand
        // var command = new SyncSearchDataCommand();
        // await _mediator.Send(command, ct);
        // return Ok(new { message = "Sync started/completed" });
        return StatusCode(501, "Implement SyncSearchDataCommand");
    }

    [HttpPost("recreate-indices")]
    public async Task<IActionResult> RecreateIndices(CancellationToken ct = default)
    {
        // نیاز به RecreateSearchIndicesCommand
        return StatusCode(501, "Implement RecreateSearchIndicesCommand");
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetIndexStats(CancellationToken ct = default)
    {
        // نیاز به GetSearchIndexStatsQuery
        return StatusCode(501, "Implement GetSearchIndexStatsQuery");
    }
}