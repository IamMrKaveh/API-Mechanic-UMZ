namespace Application.Product.Features.Queries.GetProductDetails;

public class GetProductDetailsHandler(IProductQueryService productQueryService) : IRequestHandler<GetProductDetailsQuery, ServiceResult<PublicProductDetailDto?>>
{
    private readonly IProductQueryService _productQueryService = productQueryService;

    public async Task<ServiceResult<PublicProductDetailDto?>> Handle(
        GetProductDetailsQuery request,
        CancellationToken ct)
    {
        var result = await _productQueryService.GetPublicProductDetailAsync(request.ProductId, ct);
        if (result is null)
            return ServiceResult<PublicProductDetailDto?>.NotFound("Product not found.");

        return ServiceResult<PublicProductDetailDto?>.Success(result);
    }
}