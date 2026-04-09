using Application.Brand.Adapters;
using Application.Brand.Features.Shared;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Common.ValueObjects;

namespace Application.Brand.Features.Commands.UpdateBrand;

public class UpdateBrandHandler(
    IBrandRepository brandRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<UpdateBrandCommand, ServiceResult<BrandDetailDto>>
{
    public async Task<ServiceResult<BrandDetailDto>> Handle(UpdateBrandCommand request, CancellationToken ct)
    {
        var brandId = BrandId.From(request.BrandId);
        var brand = await brandRepository.GetByIdAsync(brandId, ct);
        if (brand is null)
            return ServiceResult<BrandDetailDto>.NotFound("برند یافت نشد.");

        var rowVersion = request.RowVersion.FromBase64RowVersion();
        brandRepository.SetOriginalRowVersion(brand, rowVersion);

        var brandName = BrandName.Create(request.Name);

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slug.GenerateFrom(request.Name)
            : Slug.FromString(request.Slug);

        var uniquenessChecker = new BrandUniquenessCheckerAdapter(brandRepository);
        brand.UpdateDetails(brandName, slug, uniquenessChecker, request.Description, request.LogoPath);

        brandRepository.Update(brand);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = mapper.Map<BrandDetailDto>(brand);
        return ServiceResult<BrandDetailDto>.Success(dto);
    }
}