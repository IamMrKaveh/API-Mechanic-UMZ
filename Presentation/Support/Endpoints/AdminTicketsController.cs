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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return await Send(new GetAdminTicketsQuery(status, priority, page, pageSize), ct);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TicketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketDetails(Guid id, CancellationToken ct)
    {
        return await Send(new GetTicketDetailsQuery(id, RequestContext.UserId ?? Guid.Empty, true), ct);
    }

    [HttpPost("{id:guid}/replies")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> ReplyToTicket(
        Guid id,
        [FromBody] ReplyToTicketRequest request,
        CancellationToken ct)
    {
        return await Send(new ReplyToTicketCommand(id, request.Message), ct);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseTicket(
        Guid id,
        CancellationToken ct)
    {
        return await Send(new CloseTicketCommand(id), ct);
    }
}