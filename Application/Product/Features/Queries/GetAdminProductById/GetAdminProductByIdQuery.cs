namespace Application.Product.Features.Queries.GetAdminProductById;

public record GetAdminProductByIdQuery(Guid ProductId) : IRequest<ServiceResult<AdminProductDetailDto?>>;