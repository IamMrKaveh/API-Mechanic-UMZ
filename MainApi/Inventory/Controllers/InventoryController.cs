namespace MainApi.Inventory.Controllers;

[ApiController]
[Route("api/inventory")]
[AllowAnonymous]
public class InventoryController(IMediator mediator, ICurrentUserService currentUserService) : BaseApiController(currentUserService)
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
        if (request.VariantIds == null || !request.VariantIds.Any())
            return BadRequest("At least one variantId is required.");

        var query = new GetBatchVariantAvailabilityQuery(
            request.VariantIds.Distinct().ToList().AsReadOnly());
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}