using Application.Brand.Features.Shared;
using Application.Common.Results;

namespace Application.Brand.Features.Queries.GetBrand;

public record GetBrandQuery(int Id) : IRequest<ServiceResult<BrandDetailDto>>;