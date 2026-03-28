namespace MainApi.Support.Controllers;

[Route("api/admin/tickets")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminTicketsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

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
        var query = new GetTicketDetailsQuery(id, CurrentUser.UserId, true);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("{id}/reply")]
    public async Task<IActionResult> ReplyToTicket(int id, [FromBody] AddTicketMessageDto dto)
    {
        var command = new ReplyToTicketCommand(id, CurrentUser.UserId, dto.Message, true);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/close")]
    public async Task<IActionResult> CloseTicket(int id)
    {
        var command = new CloseTicketCommand(id, CurrentUser.UserId, true);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}