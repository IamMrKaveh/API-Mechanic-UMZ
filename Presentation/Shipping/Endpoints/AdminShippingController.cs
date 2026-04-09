using Application.Shipping.Features.Commands.CreateShipping;
using Application.Shipping.Features.Commands.DeleteShipping;
using Application.Shipping.Features.Commands.RestoreShipping;
using Application.Shipping.Features.Commands.UpdateShipping;
using Application.Shipping.Features.Queries.GetShipping;
using Application.Shipping.Features.Queries.GetShippings;
using Presentation.Shipping.Requests;

namespace Presentation.Shipping.Endpoints;

[ApiController]
[Route("api/admin/shipping")]
[Authorize(Roles = "Admin")]
public class AdminShippingsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetShippings([FromQuery] bool includeDeleted = false)
    {
        var result = await _mediator.Send(new GetShippingsQuery(includeDeleted));
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetShippingById(Guid id)
    {
        var result = await _mediator.Send(new GetShippingQuery(id));
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateShipping(
        [FromBody] CreateShippingRequest request,
        CancellationToken ct)
    {
        var command = new CreateShippingCommand(
            request.Name,
            request.BaseCost,
            request.Description,
            request.EstimatedDeliveryTime,
            request.MinDeliveryDays,
            request.MaxDeliveryDays);

        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateShipping(
        Guid id,
        [FromBody] UpdateShippingRequest request,
        CancellationToken ct)
    {
        var command = new UpdateShippingCommand(
            id,
            request.Name,
            request.BaseCost,
            request.Description,
            request.EstimatedDeliveryTime,
            request.MinDeliveryDays,
            request.MaxDeliveryDays);

        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteShipping(Guid id)
    {
        var command = new DeleteShippingCommand(id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreShipping(Guid id)
    {
        var command = new RestoreShippingCommand(id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}