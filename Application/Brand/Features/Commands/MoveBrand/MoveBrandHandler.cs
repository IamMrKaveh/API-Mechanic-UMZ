using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Application.Brand.Features.Commands.MoveBrand;

public class MoveBrandHandler(
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<MoveBrandCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(MoveBrandCommand request, CancellationToken ct)
    {
        var brandId = BrandId.From(request.BrandId);
        var brand = await brandRepository.GetByIdAsync(brandId, ct);

        if (brand is null)
            return ServiceResult.NotFound("برند یافت نشد.");

        var targetCategoryId = CategoryId.From(request.TargetCategoryId);
        var category = await categoryRepository.GetByIdAsync(targetCategoryId, ct);

        if (category is null)
            return ServiceResult.NotFound("دسته‌بندی مقصد یافت نشد.");

        brand.ChangeCategory(targetCategoryId);
        brandRepository.Update(brand);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAsync(
            "Brand",
            "MoveBrand",
            IpAddress.Unknown,
            entityType: "Brand",
            entityId: request.BrandId.ToString(),
            details: $"انتقال به دسته‌بندی {request.TargetCategoryId}",
            ct: ct);

        return ServiceResult.Success();
    }
}