using Application.Brand.Features.Shared;
using Domain.Brand.ValueObjects;

namespace Application.Brand.Features.Queries.GetBrand;

public record GetBrandQuery(BrandId Id) : IRequest<ServiceResult<PaginatedResult<BrandDetailDto>>>;