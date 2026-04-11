using Application.Support.Features.Commands.CloseTicket;
using Application.Support.Features.Commands.ReplyToTicket;
using Application.Support.Features.Queries.GetAdminTickets;
using Application.Support.Features.Queries.GetTicketDetails;
using Application.Support.Features.Shared;

namespace Presentation.Support.Endpoints;

[Route("api/admin/tickets")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminTicketsController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetTickets(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] Guid? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAdminTicketsQuery(status, priority, userId, page, pageSize);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicketDetails(Guid id)
    {
        var query = new GetTicketDetailsQuery(id, CurrentUser.UserId, true);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("{id}/reply")]
    public async Task<IActionResult> ReplyToTicket(Guid id, [FromBody] ReplyToTicketDto dto)
    {
        var command = new ReplyToTicketCommand(id, CurrentUser.UserId, dto.Message, true);
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