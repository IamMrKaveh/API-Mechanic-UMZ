namespace MainApi.User.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("summary")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        var query = new GetUserDashboardQuery(CurrentUser.UserId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}