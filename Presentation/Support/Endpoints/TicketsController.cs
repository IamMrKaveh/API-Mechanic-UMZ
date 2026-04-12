using Application.Support.Features.Commands.CloseTicket;
using Application.Support.Features.Commands.CreateTicket;
using Application.Support.Features.Commands.ReplyToTicket;
using Application.Support.Features.Queries.GetTicketDetails;
using Application.Support.Features.Queries.GetTickets;
using Presentation.Support.Requests;

namespace Presentation.Support.Endpoints;

[Route("api/tickets")]
[ApiController]
[Authorize]
public sealed class TicketsController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetMyTickets(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = new GetTicketsQuery(CurrentUser.UserId, status, priority, page, pageSize);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTicketDetails(Guid id, CancellationToken ct)
    {
        var query = new GetTicketDetailsQuery(id, CurrentUser.UserId, false);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket(
        [FromBody] CreateTicketRequest request,
        CancellationToken ct)
    {
        var command = new CreateTicketCommand(
            CurrentUser.UserId,
            request.Subject,
            request.Category,
            request.Priority,
            request.Message);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/reply")]
    public async Task<IActionResult> ReplyToTicket(
        Guid id,
        [FromBody] ReplyToTicketRequest request,
        CancellationToken ct)
    {
        var command = new ReplyToTicketCommand(id, CurrentUser.UserId, request.Message, false);
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