using Application.Brand.Features.Commands.DeleteBrand;
using Application.Common.Results;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Domain.Common.Interfaces;

namespace Application.Brand.Features.Commands.MoveBrand;

public class MoveBrandHandler(
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteBrandHandler> logger) : IRequestHandler<MoveBrandCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(MoveBrandCommand request, CancellationToken ct)
    {
        var brandId = BrandId.From(request.BrandId.Value);
        var brand = await brandRepository.GetByIdAsync(brandId, ct);
        if (brand is null)
            return ServiceResult.NotFound("Brand not found.");

        var categoryId = CategoryId.From(request.TargetCategoryId.Value);
        var category = await categoryRepository.GetByIdAsync(categoryId, ct);
        if (category is null)
            return ServiceResult.NotFound("Category not found.");

        brand.ChangeCategory(categoryId);
        brandRepository.Update(brand);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Brand {BrandId} move to {TargetCategoryId} Category successfully", request.BrandId, request.TargetCategoryId);
        return ServiceResult.Success();
    }
}