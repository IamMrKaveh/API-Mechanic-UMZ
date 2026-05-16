using Application.User.Features.Queries.GetUserDashboard;
using Application.User.Features.Shared;

namespace Presentation.User.Endpoints;

[Route("api/dashboard")]
[ApiController]
[Authorize]
public sealed class DashboardController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<UserDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken ct)
    {
        var query = new GetUserDashboardQuery(CurrentUser.UserId);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}