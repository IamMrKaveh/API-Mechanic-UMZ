using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Brand.Features.Commands.MoveBrand;

public class MoveBrandHandler(
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<MoveBrandCommand>
{
    public async Task<ServiceResult> Handle(MoveBrandCommand request, CancellationToken ct)
    {
        var brandId = BrandId.From(request.BrandId);
        var brandResult = await (brandRepository.GetByIdAsync(brandId, ct)).OrNotFoundAsync("برند یافت نشد.");
        if (brandResult.IsFailure) return brandResult.Error;
        var brand = brandResult.Value;

        var targetCategoryId = CategoryId.From(request.TargetCategoryId);
        var categoryResult = await (categoryRepository.GetByIdAsync(targetCategoryId, ct)).OrNotFoundAsync("دسته‌بندی مقصد یافت نشد.");
        if (categoryResult.IsFailure) return categoryResult.Error;
        var category = categoryResult.Value;

        brand.ChangeCategory(targetCategoryId, dateTimeProvider.UtcNow);
        brandRepository.Update(brand);

        return ServiceResult.Success();
    }
}