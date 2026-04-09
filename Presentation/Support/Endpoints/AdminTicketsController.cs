using Application.Support.Features.Commands.CloseTicket;
using Application.Support.Features.Commands.ReplyToTicket;
using Application.Support.Features.Queries.GetAdminTickets;
using Application.Support.Features.Queries.GetTicketDetails;
using Presentation.Support.Requests;

namespace Presentation.Support.Endpoints;

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
        [FromQuery] Guid? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(
            new GetAdminTicketsQuery(status, priority, userId, page, pageSize));
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicketDetails(Guid id)
    {
        var result = await _mediator.Send(
            new GetTicketDetailsQuery(id, CurrentUser.UserId, true));
        return ToActionResult(result);
    }

    [HttpPost("{id}/reply")]
    public async Task<IActionResult> ReplyToTicket(
        Guid id,
        [FromBody] ReplyToTicketRequest request)
    {
        var command = new ReplyToTicketCommand(id, CurrentUser.UserId, request.Message, true);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/close")]
    public async Task<IActionResult> CloseTicket(Guid id)
    {
        var command = new CloseTicketCommand(id, CurrentUser.UserId, true);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}