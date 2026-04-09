using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;

namespace Application.Brand.Features.Commands.DeleteBrand;

public class DeleteBrandHandler(
    IBrandRepository brandRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteBrandHandler> logger) : IRequestHandler<DeleteBrandCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(DeleteBrandCommand request, CancellationToken ct)
    {
        var brandId = BrandId.From(request.Id);
        var brand = await brandRepository.GetByIdAsync(brandId, ct);
        if (brand is null)
            return ServiceResult.NotFound("برند یافت نشد.");

        if (brand.IsActive)
            brand.Deactivate();

        brandRepository.Update(brand);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Brand {BrandId} deleted", request.Id);
        return ServiceResult.Success();
    }
}