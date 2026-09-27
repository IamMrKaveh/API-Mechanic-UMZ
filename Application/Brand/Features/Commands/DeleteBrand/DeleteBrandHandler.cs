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
        var brandResult = await (brandRepository.GetByIdAsync(brandId, ct)).OrNotFoundAsync("برند یافت نشد.");
        if (brandResult.IsFailure) return brandResult.Error;
        var brand = brandResult.Value;

        brand.Deactivate(dateTimeProvider.UtcNow);
        brandRepository.Update(brand);
        await cacheService.RemoveByPrefixAsync("brands:", ct);

        return ServiceResult.Success();
    }
}