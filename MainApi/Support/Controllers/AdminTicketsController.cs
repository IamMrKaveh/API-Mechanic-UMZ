namespace MainApi.Support.Controllers;

[Route("api/admin/tickets")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminTicketsController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminTicketsController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetTickets(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAdminTicketsQuery(status, priority, userId, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicketDetails(int id)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var query = new GetTicketDetailsQuery(id, CurrentUser.UserId.Value, true);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("{id}/reply")]
    public async Task<IActionResult> ReplyToTicket(int id, [FromBody] AddTicketMessageDto dto)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new ReplyToTicketCommand(id, CurrentUser.UserId.Value, dto.Message, true);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/close")]
    public async Task<IActionResult> CloseTicket(int id)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new CloseTicketCommand(id, CurrentUser.UserId.Value, true);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}