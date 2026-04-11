namespace Application.Product.Features.Queries.GetAdminProductById;

public record GetAdminProductByIdQuery(
    Guid ProductId,
    Guid UserId) : IRequest<ServiceResult<AdminProductDetailDto?>>;