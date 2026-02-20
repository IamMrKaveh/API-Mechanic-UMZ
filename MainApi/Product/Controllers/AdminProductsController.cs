namespace MainApi.Product.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public class AdminProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ServiceResult<PaginatedResult<AdminProductListDto>>>> GetAll([FromQuery] AdminProductSearchParams searchParams)
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

    [HttpDelete("{id}")]
    public async Task<ActionResult<ServiceResult>> Delete(int id)
    {
        var command = new DeleteProductCommand(id);
        var result = await _mediator.Send(command);
        if (result.IsFailed)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
    }
}