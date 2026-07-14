using Application.Brand.Features.Shared;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Application.Brand.Features.Commands.CreateBrand;

public sealed class CreateBrandHandler(
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IBrandUniquenessChecker brandUniquenessChecker,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IStorageService storageService)
    : ICommandHandler<CreateBrandCommand, BrandDetailDto>
{
    private const long MaxFileSizeBytes = 2 * 1024 * 1024;
    private const string EmptyLogoPlaceholder = "__EMPTY__";
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public async Task<ServiceResult<BrandDetailDto>> Handle(
        CreateBrandCommand request,
        CancellationToken ct)
    {
        var categoryId = CategoryId.From(request.CategoryId);

        var category = await categoryRepository.GetByIdAsync(categoryId, ct);
        if (category is null)
            return ServiceResult<BrandDetailDto>.NotFound("دسته‌بندی یافت نشد.");

        var brandName = BrandName.Create(request.Name);

        if (await brandRepository.ExistsByNameInCategoryAsync(brandName, categoryId, null, ct))
            return ServiceResult<BrandDetailDto>.Conflict("نام برند در این دسته‌بندی قبلاً ثبت شده است.");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? BrandSlug.GenerateFrom(request.Name)
            : BrandSlug.FromString(request.Slug);

        if (await brandRepository.ExistsBySlugAsync(slug, null, ct))
            return ServiceResult<BrandDetailDto>.Conflict("این اسلاگ قبلاً استفاده شده است.");

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

        var brand = await Domain.Brand.Aggregates.Brand.Create(
            brandName,
            slug,
            categoryId,
            brandUniquenessChecker,
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
            logoPath,
            ct);

        await brandRepository.AddAsync(brand, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = mapper.Map<BrandDetailDto>(brand);
        return ServiceResult<BrandDetailDto>.Success(dto);
    }

    private static bool HasUploadedLogo(CreateBrandCommand request)
    {
        if (request.LogoStream is null) return false;
        if (request.LogoFileSize is null or <= 0) return false;
        if (string.Equals(request.LogoFileName, EmptyLogoPlaceholder, StringComparison.Ordinal)) return false;
        return true;
    }
}