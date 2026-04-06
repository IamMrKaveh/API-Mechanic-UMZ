using Application.Brand.Features.Shared;
using Application.Common.Results;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.Interfaces;
using Domain.Common.ValueObjects;

namespace Application.Brand.Features.Commands.UpdateBrand;

public class UpdateBrandHandler(
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<UpdateBrandHandler> logger) : IRequestHandler<UpdateBrandCommand, ServiceResult<BrandDetailDto>>
{
    public async Task<ServiceResult<BrandDetailDto>> Handle(UpdateBrandCommand request, CancellationToken ct)
    {
        var brand = await brandRepository.GetByIdAsync(request.Id, ct);
        if (brand is null)
            return ServiceResult<BrandDetailDto>.NotFound("برند یافت نشد.");

        var category = await categoryRepository.GetByIdAsync(CategoryId.From(request.CategoryId), ct);
        if (category is null)
            return ServiceResult<BrandDetailDto>.NotFound("دسته‌بندی یافت نشد.");

        if (await brandRepository.ExistsByNameInCategoryAsync(request.Name, request.CategoryId, request.Id, ct))
            return ServiceResult<BrandDetailDto>.Conflict("برندی با این نام در این دسته‌بندی قبلاً ثبت شده است.");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slug.GenerateFrom(request.Name)
            : Slug.FromString(request.Slug);

        if (await brandRepository.ExistsBySlugAsync(slug.Value, request.Id, ct))
            return ServiceResult<BrandDetailDto>.Conflict("برندی با این Slug قبلاً ثبت شده است.");

        brand.UpdateDetails(BrandName.Create(request.Name), slug, request.Description, request.LogoPath);

        if (brand.CategoryId != request.CategoryId)
            brand.ChangeCategory(request.CategoryId);

        if (request.IsActive && !brand.IsActive)
            brand.Activate();
        else if (!request.IsActive && brand.IsActive)
            brand.Deactivate();

        brandRepository.Update(brand);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Brand {BrandId} updated", request.Id);
        var dto = mapper.Map<BrandDetailDto>(brand) with { CategoryName = category.Name };
        return ServiceResult<BrandDetailDto>.Success(dto);
    }
}