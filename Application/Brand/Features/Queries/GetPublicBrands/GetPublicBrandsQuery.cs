namespace Application.Brand.Features.Queries.GetPublicBrands;

public sealed record GetPublicBrandsQuery(int? CategoryId = null)
    : IRequest<ServiceResult<IReadOnlyList<BrandDto>>>;