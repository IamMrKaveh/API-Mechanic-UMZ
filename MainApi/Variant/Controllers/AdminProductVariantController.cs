namespace MainApi.Variant.Controllers;

[Route("api/admin/variants")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminProductVariantController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("by-product/{productId}")]
    public async Task<IActionResult> GetVariantsByProduct(
        int productId,
        [FromQuery] bool activeOnly = true)
    {
        var result = await _mediator.Send(new GetProductVariantsQuery(productId, activeOnly));
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddVariantCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromBody] UpdateVariantCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        [FromQuery] int productId,
        [FromQuery] int variantId)
    {
        var command = new RemoveVariantCommand(productId, variantId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/stock")]
    public async Task<IActionResult> AddStock([FromBody] AddStockCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/remove-stock")]
    public async Task<IActionResult> RemoveStock([FromBody] RemoveStockCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}