using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Application.Brand.Features.Commands.MoveBrand;

public class MoveBrandHandler(
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    ILogger<MoveBrandHandler> logger) : IRequestHandler<MoveBrandCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(MoveBrandCommand request, CancellationToken ct)
    {
        var brandId = BrandId.From(request.BrandId);
        var brand = await brandRepository.GetByIdAsync(brandId, ct);
        if (brand is null)
            return ServiceResult.NotFound("برند یافت نشد.");

        var categoryId = CategoryId.From(request.TargetCategoryId);
        var category = await categoryRepository.GetByIdAsync(categoryId, ct);
        if (category is null)
            return ServiceResult.NotFound("دسته‌بندی مقصد یافت نشد.");

        brand.ChangeCategory(categoryId);
        brandRepository.Update(brand);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Brand {BrandId} moved to category {CategoryId}", request.BrandId, request.TargetCategoryId);
        return ServiceResult.Success();
    }
}