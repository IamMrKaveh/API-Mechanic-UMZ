namespace MainApi.Variant.Controllers;

[Route("api/admin/variants")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminProductVariantController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminProductVariantController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet("by-product/{productId}")]
    public async Task<IActionResult> GetVariantsByProduct(int productId, [FromQuery] bool activeOnly = true)
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
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVariantCommand command)
    {
        if (id != command.VariantId) return BadRequest("Variant ID mismatch");
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int productId)
    {
        var command = new RemoveVariantCommand(productId, id);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/stock")]
    public async Task<IActionResult> AddStock(int id, [FromBody] AddStockCommand command)
    {
        if (id != command.VariantId) return BadRequest("Variant ID mismatch");

        if (command.UserId <= 0 && CurrentUser.UserId.HasValue)
            command = command with { UserId = CurrentUser.UserId.Value };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/remove-stock")]
    public async Task<IActionResult> RemoveStock(int id, [FromBody] RemoveStockCommand command)
    {
        if (id != command.VariantId) return BadRequest("Variant ID mismatch");

        if (command.UserId <= 0 && CurrentUser.UserId.HasValue)
            command = command with { UserId = CurrentUser.UserId.Value };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}