using Application.Brand.Features.Shared;

namespace Application.Brand.Features.Queries.GetBrandDetail;

public record GetBrandDetailQuery(Guid BrandId) : IRequest<ServiceResult<PaginatedResult<BrandDetailDto>?>>;