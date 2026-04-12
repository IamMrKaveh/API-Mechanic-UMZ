using Application.User.Features.Queries.GetUserDashboard;

namespace Presentation.User.Endpoints;

[Route("api/dashboard")]
[ApiController]
[Authorize]
public sealed class DashboardController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetUserDashboardQuery(CurrentUser.UserId), ct);
        return ToActionResult(result);
    }
}