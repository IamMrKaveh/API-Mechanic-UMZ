using Presentation.Base.Controllers.v1;
using Presentation.Inventory.Requests;

namespace Presentation.Inventory.Controllers;

[ApiController]
[Route("api/inventory")]
[AllowAnonymous]
public class InventoryController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("availability/{variantId}")]
    public async Task<IActionResult> GetVariantAvailability(int variantId)
    {
        var query = new GetVariantAvailabilityQuery(variantId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("availability/batch")]
    public async Task<IActionResult> GetBatchAvailability(
        [FromBody] BatchAvailabilityRequest request)
    {
        var query = new GetBatchVariantAvailabilityQuery(
            request.VariantIds.Distinct().ToList().AsReadOnly());
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}