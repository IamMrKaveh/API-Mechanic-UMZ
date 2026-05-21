using Application.Support.Features.Commands.CloseTicket;
using Application.Support.Features.Commands.ReplyToTicket;
using Application.Support.Features.Queries.GetAdminTickets;
using Application.Support.Features.Queries.GetTicketDetails;
using Application.Support.Features.Shared;
using Presentation.Support.Requests;

namespace Presentation.Support.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/tickets")]
[Authorize(Roles = "Admin")]
public sealed class AdminTicketsController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<TicketDto>>), StatusCodes.Status200OK)]
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
    [ProducesResponseType(typeof(ApiResponse<TicketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketDetails(Guid id, CancellationToken ct)
    {
        var query = new GetTicketDetailsQuery(id, CurrentUser.UserId, true);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/replies")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> ReplyToTicket(
        Guid id,
        [FromBody] ReplyToTicketRequest request,
        CancellationToken ct)
    {
        var command = new ReplyToTicketCommand(id, CurrentUser.UserId, request.Message, true);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
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