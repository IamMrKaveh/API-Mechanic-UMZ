using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetAdminProduct;

public record GetAdminProductQuery(
    Guid ProductId,
    Guid UserId) : IRequest<ServiceResult<AdminProductDetailDto?>>;