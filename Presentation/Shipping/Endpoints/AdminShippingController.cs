using Application.Shipping.Features.Commands.CreateShipping;
using Application.Shipping.Features.Commands.DeleteShipping;
using Application.Shipping.Features.Commands.RestoreShipping;
using Application.Shipping.Features.Commands.UpdateShipping;
using Application.Shipping.Features.Queries.GetShipping;
using Application.Shipping.Features.Queries.GetShippings;
using Application.Shipping.Features.Shared;
using MapsterMapper;

namespace Presentation.Shipping.Endpoints;

[ApiController]
[Route("api/admin/shipping")]
[Authorize(Roles = "Admin")]
public class AdminShippingsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetShippings([FromQuery] bool includeDeleted = false)
    {
        var result = await Mediator.Send(new GetShippingsQuery(includeDeleted));
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetShippingById(Guid id)
    {
        var result = await Mediator.Send(new GetShippingQuery(id));
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateShipping([FromBody] CreateShippingDto dto, CancellationToken ct)
    {
        var command = mapper.Map<CreateShippingCommand>(dto);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateShipping(Guid id, [FromBody] UpdateShippingDto dto, CancellationToken ct)
    {
        var command = mapper.Map<UpdateShippingCommand>(dto) with { Id = id };
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteShipping(Guid id)
    {
        var result = await Mediator.Send(new DeleteShippingCommand(id, CurrentUser.UserId));
        return ToActionResult(result);
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreShipping(Guid id)
    {
        var result = await Mediator.Send(new RestoreShippingCommand(id, CurrentUser.UserId));
        return ToActionResult(result);
    }
}