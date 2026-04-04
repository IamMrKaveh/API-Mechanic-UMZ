using Application.Common.Results;

namespace Application.Brand.Features.Queries.GetBrandById;

public record GetBrandByIdQuery(int Id)
    : IRequest<ServiceResult<BrandDetailDto?>>;