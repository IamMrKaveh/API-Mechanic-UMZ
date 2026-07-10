using Application.Brand.Features.Shared;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;

namespace Application.Brand.Features.Commands.UpdateBrand;

public sealed class UpdateBrandHandler(
    IBrandRepository brandRepository,
    IBrandQueryService brandQueryService,
    IBrandUniquenessChecker brandUniquenessChecker,
    IUnitOfWork unitOfWork,
    IStorageService storageService)
    : ICommandHandler<UpdateBrandCommand, BrandDetailDto>
{
    private const long MaxFileSizeBytes = 2 * 1024 * 1024;
    private const string EmptyLogoPlaceholder = "__EMPTY__";
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public async Task<ServiceResult<BrandDetailDto>> Handle(
        UpdateBrandCommand request,
        CancellationToken ct)
    {
        var brandId = BrandId.From(request.BrandId);
        var brand = await brandRepository.GetByIdAsync(brandId, ct);

        if (brand is null)
            return ServiceResult<BrandDetailDto>.NotFound("برند یافت نشد.");

        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            var rowVersion = Convert.FromBase64String(request.RowVersion);
            brandRepository.SetOriginalRowVersion(brand, rowVersion);
        }

        string? logoPath = null;

        if (HasUploadedLogo(request))
        {
            if (request.LogoFileSize > MaxFileSizeBytes)
                return ServiceResult<BrandDetailDto>.Validation("حجم فایل نمی‌تواند بیش از ۲ مگابایت باشد.");

            if (!AllowedContentTypes.Contains(request.LogoContentType, StringComparer.OrdinalIgnoreCase))
                return ServiceResult<BrandDetailDto>.Validation("فرمت فایل مجاز نیست. فقط JPEG، PNG و WebP پشتیبانی می‌شوند.");

            var extension = Path.GetExtension(request.LogoFileName);
            var fileName = $"brands/{Guid.NewGuid()}{extension}";
            logoPath = await storageService.UploadAsync(request.LogoStream!, fileName, request.LogoContentType!, "brands", ct);
        }

        var brandName = BrandName.Create(request.Name);
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? BrandSlug.GenerateFrom(request.Name)
            : BrandSlug.FromString(request.Slug);

        await brand.UpdateDetails(
            brandName,
            slug,
            brandUniquenessChecker,
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
            logoPath,
            ct);

        brandRepository.Update(brand);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = await brandQueryService.GetBrandDetailAsync(brand.Id, ct);
        if (dto is null)
            return ServiceResult<BrandDetailDto>.NotFound("برند یافت نشد.");

        return ServiceResult<BrandDetailDto>.Success(dto);
    }

    private static bool HasUploadedLogo(UpdateBrandCommand request)
    {
        if (request.LogoStream is null) return false;
        if (request.LogoFileSize is null or <= 0) return false;
        if (string.Equals(request.LogoFileName, EmptyLogoPlaceholder, StringComparison.Ordinal)) return false;
        return true;
    }
}