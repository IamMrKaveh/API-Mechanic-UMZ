using Application.Inventory.Features.Queries.GetBatchVariantAvailability;
using Application.Inventory.Features.Queries.GetVariantAvailability;
using Application.Inventory.Features.Shared;
using Presentation.Inventory.Requests;

namespace Presentation.Inventory.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/inventory")]
[AllowAnonymous]
public sealed class InventoryController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("availability/{variantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VariantAvailabilityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVariantAvailability(Guid variantId)
    {
        var query = new GetVariantAvailabilityQuery(variantId);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("availability/batch")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VariantAvailabilityDto>>), StatusCodes.Status201Created)]
    public async Task<IActionResult> GetBatchAvailability([FromBody] BatchAvailabilityRequest request)
    {
        var query = new GetBatchVariantAvailabilityQuery(request.VariantIds);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }
}