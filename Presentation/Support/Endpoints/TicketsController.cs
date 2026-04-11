using Application.Support.Features.Commands.CloseTicket;
using Application.Support.Features.Commands.CreateTicket;
using Application.Support.Features.Commands.ReplyToTicket;
using Application.Support.Features.Queries.GetTicketDetails;
using Application.Support.Features.Queries.GetTickets;
using Application.Support.Features.Shared;
using MapsterMapper;

namespace Presentation.Support.Endpoints;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TicketsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetMyTickets(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetTicketsQuery(CurrentUser.UserId, status, priority, page, pageSize);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicketDetails(Guid id)
    {
        var query = new GetTicketDetailsQuery(id, CurrentUser.UserId, false);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto dto)
    {
        var command = new CreateTicketCommand(CurrentUser.UserId, dto.Subject, dto.Category, dto.Priority, dto.Message);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/reply")]
    public async Task<IActionResult> ReplyToTicket(Guid id, [FromBody] ReplyToTicketDto dto)
    {
        var command = new ReplyToTicketCommand(id, CurrentUser.UserId, dto.Message, false);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/close")]
    public async Task<IActionResult> CloseTicket(Guid id, [FromBody] CloseTicketDto dto)
    {
        var command = new CloseTicketCommand(id, CurrentUser.UserId, dto.IsAdmin);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }
}