using Application.Brand.Features.Shared;
using Application.Common.Results;
using Domain.Brand.Aggregates;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.Interfaces;
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

        if (await brandRepository.ExistsByNameInCategoryAsync(request.Name, categoryId, null, ct))
            return ServiceResult<BrandDetailDto>.Conflict("برندی با این نام در این دسته‌بندی قبلاً ثبت شده است.");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slug.GenerateFrom(request.Name)
            : Slug.FromString(request.Slug);

        if (await brandRepository.ExistsBySlugAsync(slug.Value, null, ct))
            return ServiceResult<BrandDetailDto>.Conflict("برندی با این Slug قبلاً ثبت شده است.");

        var brandName = BrandName.Create(request.Name);

        var uniquenessChecker = new BrandUniquenessCheckerAdapter(brandRepository);
        var brand = Domain.Brand.Aggregates.Brand.Create(brandName, slug, categoryId, uniquenessChecker, request.Description, request.LogoPath);

        await brandRepository.AddAsync(brand, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = mapper.Map<BrandDetailDto>(brand);
        return ServiceResult<BrandDetailDto>.Success(dto);
    }
}