using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Common.ValueObjects;

namespace Application.Brand.Features.Commands.DeleteBrand;

public class DeleteBrandHandler(
    IBrandRepository brandRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<DeleteBrandCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(DeleteBrandCommand request, CancellationToken ct)
    {
        var brandId = BrandId.From(request.BrandId);
        var brand = await brandRepository.GetByIdAsync(brandId, ct);

        if (brand is null)
            return ServiceResult.NotFound("برند یافت نشد.");

        brand.Deactivate();
        brandRepository.Update(brand);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogAsync(
            "Brand",
            "DeleteBrand",
            IpAddress.Unknown,
            entityType: "Brand",
            entityId: request.BrandId.ToString(),
            ct: ct);

        return ServiceResult.Success();
    }
}