using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetProductDetails;

public record GetProductDetailsQuery(Guid ProductId) : IRequest<ServiceResult<PublicProductDetailDto?>>;