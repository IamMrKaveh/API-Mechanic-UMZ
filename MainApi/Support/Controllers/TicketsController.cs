using Presentation.Base.Controllers.v1;

namespace Presentation.Support.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TicketsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetMyTickets([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = new GetUserTicketsQuery(CurrentUser.UserId, status, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicketDetails(int id)
    {
        var query = new GetTicketDetailsQuery(id, CurrentUser.UserId, CurrentUser.IsAdmin);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto dto)
    {
        var command = new CreateTicketCommand(CurrentUser.UserId, dto.Subject, dto.Priority, dto.Message);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/reply")]
    public async Task<IActionResult> ReplyToTicket(int id, [FromBody] AddTicketMessageDto dto)
    {
        var command = new ReplyToTicketCommand(id, CurrentUser.UserId, dto.Message, false);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/close")]
    public async Task<IActionResult> CloseTicket(int id)
    {
        var command = new CloseTicketCommand(id, CurrentUser.UserId, false);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}