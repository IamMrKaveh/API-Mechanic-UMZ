namespace MainApi.Product.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public class AdminProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public AdminProductsController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ServiceResult<PaginatedResult<AdminProductListDto>>>> GetAll(
        [FromQuery] AdminProductSearchParams searchParams)
    {
        var query = new GetAdminProductsQuery(searchParams);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResult<AdminProductViewDto?>>> GetById(int id)
    {
        var query = new GetAdminProductByIdQuery(id);
        var result = await _mediator.Send(query);
        if (result.IsFailed)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpGet("{id}/detail")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var result = await _mediator.Send(new GetAdminProductDetailQuery(id));
        if (result.IsFailed) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductInput input)
    {
        var command = new CreateProductCommand(input);
        var productId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = productId }, productId);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductInput input)
    {
        if (id != input.Id) return BadRequest();

        var command = new UpdateProductCommand(input);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPut("{id}/details")]
    public async Task<IActionResult> UpdateDetails(int id, [FromBody] UpdateProductDetailsCommand command)
    {
        if (id != command.Id) return BadRequest();
        var result = await _mediator.Send(command);
        if (result.IsFailed) return StatusCode(result.StatusCode, result);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ServiceResult>> Delete(int id)
    {
        var command = new DeleteProductCommand(id);
        var result = await _mediator.Send(command);
        if (result.IsFailed)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        if (!_currentUserService.UserId.HasValue) return Unauthorized();
        var result = await _mediator.Send(new RestoreProductCommand(id, _currentUserService.UserId.Value));
        if (result.IsFailed) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }

    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await _mediator.Send(new ActivateProductCommand(id));
        if (result.IsFailed) return StatusCode(result.StatusCode, result);
        return NoContent();
    }

    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _mediator.Send(new DeactivateProductCommand(id));
        if (result.IsFailed) return StatusCode(result.StatusCode, result);
        return NoContent();
    }

    [HttpPatch("{id}/price")]
    public async Task<IActionResult> ChangePrice(int id, [FromBody] ChangePriceCommand command)
    {
        if (id != command.ProductId) return BadRequest();
        var result = await _mediator.Send(command);
        if (result.IsFailed) return StatusCode(result.StatusCode, result);
        return NoContent();
    }

    [HttpPost("bulk-update-prices")]
    public async Task<IActionResult> BulkUpdatePrices([FromBody] BulkUpdatePricesCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailed) return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}