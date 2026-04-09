using Application.Brand.Features.Shared;

namespace Application.Brand.Features.Queries.GetBrand;

public record GetBrandQuery(Guid Id) : IRequest<ServiceResult<BrandDetailDto>>;