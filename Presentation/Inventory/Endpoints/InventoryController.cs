using Application.Inventory.Features.Queries.GetBatchVariantAvailability;
using Application.Inventory.Features.Queries.GetVariantAvailability;
using Presentation.Inventory.Requests;

namespace Presentation.Inventory.Endpoints;

[ApiController]
[Route("api/inventory")]
[AllowAnonymous]
public sealed class InventoryController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("availability/{variantId:guid}")]
    public async Task<IActionResult> GetVariantAvailability(Guid variantId)
    {
        var result = await Mediator.Send(new GetVariantAvailabilityQuery(variantId));
        return ToActionResult(result);
    }

    [HttpPost("availability/batch")]
    public async Task<IActionResult> GetBatchAvailability([FromBody] BatchAvailabilityRequest request)
    {
        var result = await Mediator.Send(new GetBatchVariantAvailabilityQuery(request.VariantIds));
        return ToActionResult(result);
    }
}