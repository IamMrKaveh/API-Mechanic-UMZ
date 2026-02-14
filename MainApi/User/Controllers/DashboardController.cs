namespace MainApi.User.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController : BaseApiController
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var query = new GetUserDashboardQuery(CurrentUser.UserId.Value);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}