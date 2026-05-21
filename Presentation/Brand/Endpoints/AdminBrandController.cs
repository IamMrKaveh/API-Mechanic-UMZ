using Application.Brand.Features.Commands.CreateBrand;
using Application.Brand.Features.Commands.DeleteBrand;
using Application.Brand.Features.Commands.MoveBrand;
using Application.Brand.Features.Commands.UpdateBrand;
using Application.Brand.Features.Queries.GetAdminBrands;
using Application.Brand.Features.Queries.GetBrandDetail;
using Application.Brand.Features.Shared;
using Application.Common.Results;
using Application.Media.Contracts;
using Presentation.Brand.Requests;

namespace Presentation.Brand.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/brands")]
[Authorize(Roles = "Admin")]
public sealed class AdminBrandController(
    IMediator mediator,
    IMapper mapper,
    IStorageService storageService) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<BrandListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBrands(
        [FromQuery] GetAdminBrandsRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetAdminBrandsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BrandDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBrand(Guid id, CancellationToken ct)
    {
        var query = new GetBrandDetailQuery(id);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BrandDetailDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBrand(
        [FromForm] CreateBrandRequest request,
        CancellationToken ct)
    {
        var logoPath = await UploadLogoIfPresentAsync(request.LogoFile, ct);
        if (logoPath.IsFailed)
            return ToActionResult(logoPath);

        var command = new CreateBrandCommand(
            request.CategoryId,
            request.Name,
            request.Slug,
            request.Description,
            logoPath.Value);

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
        var logoPath = await UploadLogoIfPresentAsync(request.LogoFile, ct);
        if (logoPath.IsFailed)
            return ToActionResult(logoPath);

        var command = new UpdateBrandCommand(
            id,
            request.CategoryId,
            request.Name,
            request.Slug,
            request.Description,
            logoPath.Value,
            request.RowVersion);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBrand(Guid id, CancellationToken ct)
    {
        var command = new DeleteBrandCommand(id, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("move")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MoveBrand(
        [FromBody] MoveBrandRequest request,
        CancellationToken ct)
    {
        var command = Mapper.Map<MoveBrandCommand>(request);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    private async Task<ServiceResult<string?>> UploadLogoIfPresentAsync(
        IFormFile? file,
        CancellationToken ct)
    {
        if (file is null)
            return ServiceResult<string?>.Success(null);

        const long maxFileSizeBytes = 2 * 1024 * 1024;
        string[] allowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

        if (file.Length > maxFileSizeBytes)
            return ServiceResult<string?>.Validation("حجم فایل نمی‌تواند بیش از ۲ مگابایت باشد.");

        if (!allowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return ServiceResult<string?>.Validation("فرمت فایل مجاز نیست. فقط JPEG، PNG و WebP پشتیبانی می‌شوند.");

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"brands/{Guid.NewGuid()}{extension}";

        await using var stream = file.OpenReadStream();
        var path = await storageService.UploadAsync(stream, fileName, file.ContentType, "brands", ct);

        return ServiceResult<string?>.Success(path);
    }
}