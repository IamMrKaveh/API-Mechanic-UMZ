using Application.Brand.Adapters;
using Application.Brand.Features.Shared;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Common.ValueObjects;
using Mapster;

namespace Application.Brand.Features.Commands.UpdateBrand;

public sealed class UpdateBrandHandler(
    IBrandRepository brandRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateBrandCommand, ServiceResult<BrandDetailDto>>
{
    public async Task<ServiceResult<BrandDetailDto>> Handle(
        UpdateBrandCommand request,
        CancellationToken ct)
    {
        var brandId = BrandId.From(request.BrandId);
        var brand = await brandRepository.GetByIdAsync(brandId, ct);

        if (brand is null)
            return ServiceResult<BrandDetailDto>.NotFound("برند یافت نشد.");

        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            var rowVersion = Convert.FromBase64String(request.RowVersion);
            brandRepository.SetOriginalRowVersion(brand, rowVersion);
        }

        var brandName = BrandName.Create(request.Name);
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slug.GenerateFrom(request.Name)
            : Slug.FromString(request.Slug);

        var uniquenessChecker = new BrandUniquenessCheckerAdapter(brandRepository);
        brand.UpdateDetails(brandName, slug, uniquenessChecker, request.Description, request.LogoPath);

        brandRepository.Update(brand);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = brand.Adapt<BrandDetailDto>();
        return ServiceResult<BrandDetailDto>.Success(dto);
    }
}