using Application.Brand.Features.Shared;

namespace Application.Brand.Features.Queries.GetPublicBrands;

public sealed record GetPublicBrandsQuery(Guid? CategoryId) : IRequest<ServiceResult<PaginatedResult<BrandDto>>>;