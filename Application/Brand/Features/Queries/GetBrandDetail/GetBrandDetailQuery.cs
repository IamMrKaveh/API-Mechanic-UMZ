using Application.Common.Models;

namespace Application.Brand.Features.Queries.GetBrandDetail;

public record GetBrandDetailQuery(
    int GroupId
    ) : IRequest<ServiceResult<BrandDetailDto?>>;