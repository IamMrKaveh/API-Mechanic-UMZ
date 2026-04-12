using Application.Search.Features.Commands.RecreateSearchIndices;
using Application.Search.Features.Commands.SyncSearchData;
using Application.Search.Features.Queries.GetSearchIndexStats;
using MapsterMapper;

namespace Presentation.Search.Endpoints;

[ApiController]
[Route("api/admin/search")]
[Authorize(Roles = "Admin")]
public class AdminSearchController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpPost("sync")]
    public async Task<IActionResult> SyncAllData(CancellationToken ct)
    {
        var command = new SyncSearchDataCommand();
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("recreate-indices")]
    public async Task<IActionResult> RecreateIndices(CancellationToken ct)
    {
        var command = new RecreateSearchIndicesCommand();
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetIndexStats(CancellationToken ct)
    {
        var query = new GetSearchIndexStatsQuery();
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}