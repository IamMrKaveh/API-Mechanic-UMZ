using Application.Common.Results;
using Domain.Brand.Interfaces;
using Domain.Category.Interfaces;
using Domain.Common.Interfaces;

namespace Application.Brand.Features.Commands.MoveBrand;

public class MoveBrandHandler(
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<MoveBrandCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(MoveBrandCommand request, CancellationToken ct)
    {
        var brand = await brandRepository.GetByIdAsync(request.BrandId, ct);
        if (brand is null)
            return ServiceResult.NotFound("برند یافت نشد.");

        var category = await categoryRepository.GetByIdAsync(request.TargetCategoryId, ct);
        if (category is null)
            return ServiceResult.NotFound("دسته‌بندی مقصد یافت نشد.");

        brand.ChangeCategory(request.TargetCategoryId);
        brandRepository.Update(brand);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}