using Application.Brand.Features.Shared;

namespace Application.Brand.Features.Queries.GetPublicBrands;

public sealed record GetPublicBrandsQuery(Guid? CategoryId) : IQuery<IReadOnlyList<BrandListItemDto>>;