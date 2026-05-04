using Application.Brand.Adapters;
using Application.Brand.Features.Shared;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Application.Brand.Features.Commands.CreateBrand;

public sealed class CreateBrandHandler(
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IAuditService auditService) : IRequestHandler<CreateBrandCommand, ServiceResult<BrandDetailDto>>
{
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
            ? Slug.GenerateFrom(request.Name)
            : Slug.FromString(request.Slug);

        if (await brandRepository.ExistsBySlugAsync(slug, null, ct))
            return ServiceResult<BrandDetailDto>.Conflict("این اسلاگ قبلاً استفاده شده است.");

        var uniquenessChecker = new BrandUniquenessCheckerAdapter(brandRepository);

        var brand = Domain.Brand.Aggregates.Brand.Create(
            brandName,
            slug,
            categoryId,
            uniquenessChecker,
            request.Description,
            request.LogoPath);

        await brandRepository.AddAsync(brand, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAsync(
            "Brand",
            "CreateBrand",
            IpAddress.Unknown,
            entityType: "Brand",
            entityId: brand.Id.Value.ToString(),
            ct: ct);

        var dto = mapper.Map<BrandDetailDto>(brand);
        return ServiceResult<BrandDetailDto>.Success(dto);
    }
}