using Application.Shipping.Features.Commands.CreateShipping;
using Application.Shipping.Features.Commands.DeleteShipping;
using Application.Shipping.Features.Commands.RestoreShipping;
using Application.Shipping.Features.Commands.UpdateShipping;
using Application.Shipping.Features.Queries.GetShipping;
using Application.Shipping.Features.Queries.GetShippings;
using MapsterMapper;
using Presentation.Shipping.Requests;

namespace Presentation.Shipping.Endpoints;

[ApiController]
[Route("api/admin/shipping")]
[Authorize(Roles = "Admin")]
public sealed class AdminShippingsController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
    [HttpGet]
    public async Task<IActionResult> GetShippings(
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetShippingsQuery(includeDeleted), ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetShippingById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetShippingQuery(id), ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateShipping(
        [FromBody] CreateShippingRequest request,
        CancellationToken ct)
    {
        var command = Mapper.Map<CreateShippingCommand>(request);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateShipping(
        Guid id,
        [FromBody] UpdateShippingRequest request,
        CancellationToken ct)
    {
        var command = Mapper.Map<UpdateShippingCommand>(request) with { Id = id };
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteShipping(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new DeleteShippingCommand(id, CurrentUser.UserId), ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> RestoreShipping(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new RestoreShippingCommand(id, CurrentUser.UserId), ct);
        return ToActionResult(result);
    }
}