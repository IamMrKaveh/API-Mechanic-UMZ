namespace Application.Product.Features.Queries.GetProductDetails;

public record GetProductDetailsQuery(int ProductId)
    : IRequest<ServiceResult<PublicProductDetailDto?>>;