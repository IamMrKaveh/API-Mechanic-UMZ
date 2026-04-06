using Presentation.Base.Controllers.v1;
using Presentation.Brand.Requests;

namespace Presentation.Brand.Controllers;

[Route("api/admin/brand")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminBrandController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

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
    public async Task<IActionResult> CreateBrand([FromForm] CreateBrandRequest request)
    {
        var command = new CreateBrandCommand
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Description = request.Description,
            IconFile = ToFileDto(request.IconFile)
        };
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetBrand), new { id = result.Value }, result.Value);
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
    public async Task<IActionResult> UpdateBrand(int id, [FromForm] UpdateBrandRequest request)
    {
        var command = new UpdateBrandCommand
        {
            CategoryId = request.CategoryId,
            BrandId = id,
            Name = request.Name,
            Description = request.Description,
            IconFile = ToFileDto(request.IconFile),
            RowVersion = request.RowVersion
        };
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
    public async Task<IActionResult> MoveBrand([FromBody] MoveBrandRequest request)
    {
        var command = new MoveBrandCommand(request.SourceCategoryId, request.TargetCategoryId, request.BrandId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    private static FileDto? ToFileDto(IFormFile? file)
    {
        if (file == null) return null;
        return new FileDto
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Content = file.OpenReadStream()
        };
    }
}