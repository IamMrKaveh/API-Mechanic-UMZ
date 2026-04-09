using Application.Brand.Adapters;
using Application.Brand.Features.Shared;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.ValueObjects;

namespace Application.Brand.Features.Commands.CreateBrand;

public class CreateBrandHandler(
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<CreateBrandHandler> logger) : IRequestHandler<CreateBrandCommand, ServiceResult<BrandDetailDto>>
{
    public async Task<ServiceResult<BrandDetailDto>> Handle(CreateBrandCommand request, CancellationToken ct)
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

        logger.LogInformation("Brand {BrandName} created with ID {BrandId}", brand.Name, brand.Id);

        var dto = mapper.Map<BrandDetailDto>(brand);
        return ServiceResult<BrandDetailDto>.Success(dto);
    }
}