using Application.Brand.Features.Commands.CreateBrand;
using Application.Brand.Features.Commands.DeleteBrand;
using Application.Brand.Features.Commands.MoveBrand;
using Application.Brand.Features.Commands.UpdateBrand;
using Application.Brand.Features.Queries.GetAdminBrands;
using Application.Brand.Features.Queries.GetBrandDetail;
using Application.Brand.Features.Shared;
using Presentation.Brand.Requests;

namespace Presentation.Brand.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/brands")]
[Authorize(Roles = "Admin")]
public sealed class AdminBrandController(
    IMediator mediator,
    IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<BrandListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBrands(
        [FromQuery] GetAdminBrandsRequest request,
        CancellationToken ct)
    {
        return await Send(Mapper.Map<GetAdminBrandsQuery>(request), ct);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BrandDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBrand(Guid id, CancellationToken ct)
    {
        return await Send(new GetBrandDetailQuery(id), ct);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BrandDetailDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBrand(
        [FromForm] CreateBrandRequest request,
        CancellationToken ct)
    {
        var command = new CreateBrandCommand(
            request.CategoryId,
            request.Name,
            request.Slug,
            request.Description,
            request.LogoFile?.OpenReadStream(),
            request.LogoFile?.FileName,
            request.LogoFile?.ContentType,
            request.LogoFile?.Length);

        var result = await Mediator.Send(command, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetBrand), new { id = result.Value!.Id }, result.Value);

        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BrandDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BrandDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BrandDetailDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateBrand(
        Guid id,
        [FromForm] UpdateBrandRequest request,
        CancellationToken ct)
    {
        var command = new UpdateBrandCommand(
            id,
            request.CategoryId,
            request.Name,
            request.Slug,
            request.Description,
            request.LogoFile?.OpenReadStream(),
            request.LogoFile?.FileName,
            request.LogoFile?.ContentType,
            request.LogoFile?.Length,
            request.RowVersion);

        return await Send(command, ct);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBrand(Guid id, CancellationToken ct)
    {
        return await Send(new DeleteBrandCommand(id), ct);
    }

    [HttpPatch("move")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MoveBrand(
        [FromBody] MoveBrandRequest request,
        CancellationToken ct)
    {
        return await Send(Mapper.Map<MoveBrandCommand>(request), ct);
    }
}