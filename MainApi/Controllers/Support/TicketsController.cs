using Application.Common.Interfaces.User;
using Application.DTOs.Support;
using MainApi.Controllers.Base;

namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TicketsController : BaseApiController
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService, ICurrentUserService currentUserService) : base(currentUserService)
    {
        _ticketService = ticketService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyTickets()
    {
        var userId = CurrentUser.UserId;
        if (userId == null) return Unauthorized();

        var result = await _ticketService.GetUserTicketsAsync(userId.Value);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicketDetails(int id)
    {
        var userId = CurrentUser.UserId;
        if (userId == null) return Unauthorized();

        var result = await _ticketService.GetTicketDetailsAsync(userId.Value, id);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto dto)
    {
        var userId = CurrentUser.UserId;
        if (userId == null) return Unauthorized();

        var result = await _ticketService.CreateTicketAsync(userId.Value, dto);
        if (result.Success && result.Data != null)
        {
            return CreatedAtAction(nameof(GetTicketDetails), new { id = result.Data.Id }, result.Data);
        }
        return ToActionResult(result);
    }

    [HttpPost("{id}/reply")]
    public async Task<IActionResult> ReplyToTicket(int id, [FromBody] AddTicketMessageDto dto)
    {
        var userId = CurrentUser.UserId;
        if (userId == null) return Unauthorized();

        var result = await _ticketService.AddMessageAsync(userId.Value, id, dto);
        return ToActionResult(result);
    }
}