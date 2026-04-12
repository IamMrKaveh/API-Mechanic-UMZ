using Application.Support.Features.Commands.CloseTicket;
using Application.Support.Features.Commands.ReplyToTicket;
using Application.Support.Features.Queries.GetAdminTickets;
using Application.Support.Features.Queries.GetTicketDetails;
using Presentation.Support.Requests;

namespace Presentation.Support.Endpoints;

[Route("api/admin/tickets")]
[ApiController]
[Authorize(Roles = "Admin")]
public sealed class AdminTicketsController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetTickets(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] Guid? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new GetAdminTicketsQuery(status, priority, userId, page, pageSize);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTicketDetails(Guid id, CancellationToken ct)
    {
        var query = new GetTicketDetailsQuery(id, CurrentUser.UserId, true);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/reply")]
    public async Task<IActionResult> ReplyToTicket(
        Guid id,
        [FromBody] ReplyToTicketRequest request,
        CancellationToken ct)
    {
        var command = new ReplyToTicketCommand(id, CurrentUser.UserId, request.Message, true);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> CloseTicket(
        Guid id,
        [FromBody] CloseTicketRequest request,
        CancellationToken ct)
    {
        var command = new CloseTicketCommand(id, CurrentUser.UserId, request.IsAdmin);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}