namespace Application.Product.Features.Queries.GetAdminProductById;

public record GetAdminProductByIdQuery(int ProductId) : IRequest<ServiceResult<AdminProductDetailDto?>>;