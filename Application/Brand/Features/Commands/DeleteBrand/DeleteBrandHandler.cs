using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;

namespace Application.Brand.Features.Commands.DeleteBrand;

public class DeleteBrandHandler(
    IBrandRepository brandRepository)
    : ICommandHandler<DeleteBrandCommand>
{
    public async Task<ServiceResult> Handle(DeleteBrandCommand request, CancellationToken ct)
    {
        var brandId = BrandId.From(request.BrandId);
        var brand = await brandRepository.GetByIdAsync(brandId, ct);

        if (brand is null)
            return ServiceResult.NotFound("برند یافت نشد.");

        brand.Deactivate();
        brandRepository.Update(brand);

        return ServiceResult.Success();
    }
}