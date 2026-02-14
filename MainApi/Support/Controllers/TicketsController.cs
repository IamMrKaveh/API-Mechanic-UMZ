namespace MainApi.Support.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TicketsController : BaseApiController
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyTickets([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var query = new GetUserTicketsQuery(CurrentUser.UserId.Value, status, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicketDetails(int id)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var query = new GetTicketDetailsQuery(id, CurrentUser.UserId.Value, CurrentUser.IsAdmin);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto dto)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new CreateTicketCommand(CurrentUser.UserId.Value, dto.Subject, dto.Priority, dto.Message);
        var result = await _mediator.Send(command);

        if (result.IsSucceed)
        {
            return CreatedAtAction(nameof(GetTicketDetails), new { id = result.Data }, result.Data);
        }
        return ToActionResult(result);
    }

    [HttpPost("{id}/reply")]
    public async Task<IActionResult> ReplyToTicket(int id, [FromBody] AddTicketMessageDto dto)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new ReplyToTicketCommand(id, CurrentUser.UserId.Value, dto.Message, false);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/close")]
    public async Task<IActionResult> CloseTicket(int id)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new CloseTicketCommand(id, CurrentUser.UserId.Value, false);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}