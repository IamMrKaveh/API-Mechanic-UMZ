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
    public async Task<ActionResult<ServiceResult<AdminProductViewDto>>> Create([FromForm] CreateProductCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailed)
            return StatusCode(result.StatusCode, result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ServiceResult<AdminProductViewDto>>> Update(int id, [FromForm] UpdateProductCommand command)
    {
        if (id != command.Id) return BadRequest();
        var result = await _mediator.Send(command);
        if (result.IsFailed)
            return StatusCode(result.StatusCode, result);
        return Ok(result);
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