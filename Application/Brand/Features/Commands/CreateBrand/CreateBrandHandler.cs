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
        var category = await categoryRepository.GetByIdAsync(CategoryId.From(request.CategoryId), ct);
        if (category is null)
            return ServiceResult<BrandDetailDto>.NotFound("دسته‌بندی یافت نشد.");

        if (await brandRepository.ExistsByNameInCategoryAsync(request.Name, request.CategoryId, null, ct))
            return ServiceResult<BrandDetailDto>.Conflict("برندی با این نام در این دسته‌بندی قبلاً ثبت شده است.");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slug.GenerateFrom(request.Name)
            : Slug.FromString(request.Slug);

        if (await brandRepository.ExistsBySlugAsync(slug.Value, null, ct))
            return ServiceResult<BrandDetailDto>.Conflict("برندی با این Slug قبلاً ثبت شده است.");

        var brand = Brand.Create(
            BrandName.Create(request.Name),
            slug,
            request.CategoryId,
            request.Description,
            request.LogoPath);

        await brandRepository.AddAsync(brand, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Brand {Name} created with ID {Id}", brand.Name.Value, brand.Id.Value);

        var dto = mapper.Map<BrandDetailDto>(brand) with { CategoryName = category.Name };
        return ServiceResult<BrandDetailDto>.Success(dto);
    }
}