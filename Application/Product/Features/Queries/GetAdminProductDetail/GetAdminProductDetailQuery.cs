using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetAdminProductDetail;

public record GetAdminProductDetailQuery(
    Guid ProductId) : IRequest<ServiceResult<AdminProductDetailDto?>>;