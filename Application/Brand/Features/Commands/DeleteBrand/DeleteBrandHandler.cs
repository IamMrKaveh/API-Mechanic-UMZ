using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Brand.Features.Commands.DeleteBrand;

public class DeleteBrandHandler(
    IBrandRepository brandRepository,
    ICacheService cacheService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<DeleteBrandCommand>
{
    public async Task<ServiceResult> Handle(DeleteBrandCommand request, CancellationToken ct)
    {
        var brandId = BrandId.From(request.BrandId);
        var brand = await brandRepository.GetByIdAsync(brandId, ct);

        if (brand is null)
            return ServiceResult.NotFound("برند یافت نشد.");

        brand.Deactivate(dateTimeProvider.UtcNow);
        brandRepository.Update(brand);
        await cacheService.RemoveByPrefixAsync("brands:", ct);

        return ServiceResult.Success();
    }
}