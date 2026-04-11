namespace Application.Product.Features.Queries.GetAdminProductDetail;

public record GetAdminProductDetailQuery(
    Guid ProductId,
    Guid UserId) : IRequest<ServiceResult<AdminProductDetailDto?>>;