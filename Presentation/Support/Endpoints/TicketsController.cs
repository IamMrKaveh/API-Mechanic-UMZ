using Application.Support.Features.Commands.CloseTicket;
using Application.Support.Features.Commands.CreateTicket;
using Application.Support.Features.Commands.ReplyToTicket;
using Application.Support.Features.Queries.GetTicketDetails;
using Application.Support.Features.Queries.GetTickets;
using Presentation.Support.Requests;

namespace Presentation.Support.Endpoints;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TicketsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetMyTickets(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(
            new GetTicketsQuery(
                CurrentUser.UserId,
                status,
                priority,
                page,
                pageSize));
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicketDetails(Guid id)
    {
        var result = await _mediator.Send(
            new GetTicketDetailsQuery(id, CurrentUser.UserId, CurrentUser.IsAdmin));
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var command = new CreateTicketCommand(
            CurrentUser.UserId,
            request.Subject,
            request.Priority,
            request.Message);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/reply")]
    public async Task<IActionResult> ReplyToTicket(
        Guid id,
        [FromBody] ReplyToTicketRequest request)
    {
        var command = new ReplyToTicketCommand(id, CurrentUser.UserId, request.Message, false);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/close")]
    public async Task<IActionResult> CloseTicket(Guid id)
    {
        var command = new CloseTicketCommand(id, CurrentUser.UserId, false);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}