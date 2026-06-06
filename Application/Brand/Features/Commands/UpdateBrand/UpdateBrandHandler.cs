using Application.Brand.Adapters;
using Application.Brand.Features.Shared;
using Application.Media.Contracts;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;

namespace Application.Brand.Features.Commands.UpdateBrand;

public sealed class UpdateBrandHandler(
    IBrandRepository brandRepository,
    IUnitOfWork unitOfWork,
    IStorageService storageService) : IRequestHandler<UpdateBrandCommand, ServiceResult<BrandDetailDto>>
{
    private const long MaxFileSizeBytes = 2 * 1024 * 1024;
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

        if (request.LogoStream is not null)
        {
            if (request.LogoFileSize > MaxFileSizeBytes)
                return ServiceResult<BrandDetailDto>.Validation("حجم فایل نمی‌تواند بیش از ۲ مگابایت باشد.");

            if (!AllowedContentTypes.Contains(request.LogoContentType, StringComparer.OrdinalIgnoreCase))
                return ServiceResult<BrandDetailDto>.Validation("فرمت فایل مجاز نیست. فقط JPEG، PNG و WebP پشتیبانی می‌شوند.");

            var extension = Path.GetExtension(request.LogoFileName);
            var fileName = $"brands/{Guid.NewGuid()}{extension}";
            logoPath = await storageService.UploadAsync(request.LogoStream, fileName, request.LogoContentType!, "brands", ct);
        }

        var brandName = BrandName.Create(request.Name);
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slug.GenerateFrom(request.Name)
            : Slug.FromString(request.Slug);

        var uniquenessChecker = new BrandUniquenessCheckerAdapter(brandRepository);
        brand.UpdateDetails(brandName, slug, uniquenessChecker, request.Description, logoPath);

        brandRepository.Update(brand);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = brand.Adapt<BrandDetailDto>();
        return ServiceResult<BrandDetailDto>.Success(dto);
    }
}