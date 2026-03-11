namespace MainApi.Product.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public class AdminProductsController(IMediator mediator, ICurrentUserService currentUserService) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ICurrentUserService _currentUserService = currentUserService;

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
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.Description,
            request.Price,
            request.CategoryId,
            request.BrandId);

        var result = await _mediator.Send(command, ct);
        return ToCreatedResult(result, nameof(GetById), new { productId = result.Value });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductInput input)
    {
        if (id != input.Id) return BadRequest();

        var command = new UpdateProductCommand(input);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPut("{productId:int}/details")]
    public async Task<IActionResult> UpdateDetails(int productId, [FromBody] UpdateProductDetailsRequest request, CancellationToken ct)
    {
        var command = new UpdateProductDetailsCommand(productId, request.Name, request.Description, request.CategoryId, request.BrandId);
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
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

    [HttpPut("{productId:int}/price")]
    public async Task<IActionResult> ChangePrice(int productId, [FromBody] ChangePriceRequest request, CancellationToken ct)
    {
        var command = new ChangePriceCommand(productId, request.NewPrice);
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("prices/bulk")]
    public async Task<IActionResult> BulkUpdatePrices([FromBody] BulkUpdatePricesRequest request, CancellationToken ct)
    {
        var items = request.Items.Select(i => new PriceUpdateItem(i.ProductId, i.NewPrice)).ToList();
        var command = new BulkUpdatePricesCommand(items);
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }
}