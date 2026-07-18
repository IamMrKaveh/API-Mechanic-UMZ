using Application.Support.Features.Commands.CloseTicket;
using Application.Support.Features.Commands.CreateTicket;
using Application.Support.Features.Commands.ReplyToTicket;
using Application.Support.Features.Queries.GetTicketDetails;
using Application.Support.Features.Queries.GetTickets;
using Application.Support.Features.Shared;
using Presentation.Support.Requests;

namespace Presentation.Support.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/tickets")]
[Authorize]
public sealed class TicketsController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<TicketListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTickets(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        return await Send(new GetTicketsQuery(status, priority, page, pageSize), ct);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TicketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketDetails(Guid id, CancellationToken ct)
    {
        return await Send(new GetTicketDetailsQuery(id, false), ct);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TicketDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTicket(
        [FromBody] CreateTicketRequest request,
        CancellationToken ct)
    {
        return await Send(
            new CreateTicketCommand(request.Subject, request.Category, request.Priority, request.Message),
            ct);
    }

    [HttpPost("{id:guid}/replies")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
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
    public async Task<IActionResult> CloseTicket(Guid id, CancellationToken ct)
    {
        return await Send(new CloseTicketCommand(id), ct);
    }
}