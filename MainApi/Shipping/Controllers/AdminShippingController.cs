using Application.Shipping.Features.Commands.CreateShipping;
using Application.Shipping.Features.Commands.DeleteShipping;
using Application.Shipping.Features.Commands.RestoreShipping;
using Application.Shipping.Features.Commands.UpdateShipping;
using Application.Shipping.Features.Queries.GetShippingById;
using Application.Shipping.Features.Queries.GetShippings;

namespace MainApi.Shipping.Controllers;

[ApiController]
[Route("api/admin/shipping")]
[Authorize(Roles = "Admin")]
public class AdminShippingsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetShippings([FromQuery] bool includeDeleted = false)
    {
        var query = new GetShippingsQuery(includeDeleted);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetShippingById(int id)
    {
        var result = await _mediator.Send(new GetShippingQuery(id));
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateShipping([FromBody] CreateShippingCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateShipping(int id, [FromBody] UpdateShippingCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteShipping(int id)
    {
        var command = new DeleteShippingCommand(id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreShipping(int id)
    {
        var command = new RestoreShippingCommand(id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}