using Application.Brand.Features.Commands.CreateBrand;
using Application.Brand.Features.Commands.DeleteBrand;
using Application.Brand.Features.Commands.MoveBrand;
using Application.Brand.Features.Commands.UpdateBrand;
using Application.Brand.Features.Queries.GetAdminBrands;
using Application.Brand.Features.Queries.GetBrandDetail;
using Presentation.Brand.Requests;

namespace Presentation.Brand.Endpoints;

[Route("api/admin/brand")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminBrandController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetBrands(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(
            new GetAdminBrandsQuery(categoryId, search, isActive, includeDeleted, page, pageSize));
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBrand([FromForm] CreateBrandRequest request)
    {
        var command = new CreateBrandCommand(
            request.CategoryId,
            request.Name,
            request.Slug,
            request.Description,
            null);

        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetBrand), new { id = result.Value }, result.Value);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBrand(Guid id)
    {
        var result = await _mediator.Send(new GetBrandDetailQuery(id));
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBrand(Guid id, [FromForm] UpdateBrandRequest request)
    {
        var command = new UpdateBrandCommand(
            id,
            request.CategoryId,
            request.Name,
            request.Slug,
            request.Description,
            null,
            request.RowVersion);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBrand(Guid id)
    {
        var command = new DeleteBrandCommand(id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("move")]
    public async Task<IActionResult> MoveBrand([FromBody] MoveBrandRequest request)
    {
        var command = new MoveBrandCommand(request.BrandId, request.TargetCategoryId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}