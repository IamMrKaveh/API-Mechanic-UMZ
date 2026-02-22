namespace MainApi.Brand.Controllers;

[Route("api/admin/brand")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminBrandController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminBrandController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetBrands(
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAdminBrandsQuery(categoryId, search, isActive, includeDeleted, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBrand([FromForm] CreateBrandCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSucceed)
        {
            return CreatedAtAction(nameof(GetBrand), new { id = result.Data }, result.Data);
        }
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBrand(int id)
    {
        var query = new GetBrandDetailQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBrand(int id, [FromForm] UpdateBrandCommand command)
    {
        if (id != command.BrandId) return BadRequest("Mismatched Group ID"); // Note: Command uses GroupId

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBrand(int id, [FromQuery] int categoryId)
    {
        var command = new DeleteBrandCommand(categoryId, id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("move")]
    public async Task<IActionResult> MoveBrand([FromBody] MoveBrandCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}