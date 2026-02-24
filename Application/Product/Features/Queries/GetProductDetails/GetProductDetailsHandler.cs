namespace Application.Product.Features.Queries.GetProductDetails;

public class GetProductDetailsHandler
    : IRequestHandler<GetProductDetailsQuery, ServiceResult<PublicProductDetailDto?>>
{
    private readonly IProductQueryService _productQueryService;

    public GetProductDetailsHandler(IProductQueryService productQueryService)
    {
        _productQueryService = productQueryService;
    }

    public async Task<ServiceResult<PublicProductDetailDto?>> Handle(
        GetProductDetailsQuery request, CancellationToken ct)
    {
        var result = await _productQueryService.GetPublicProductDetailAsync(request.ProductId, ct);
        if (result == null)
            return ServiceResult<PublicProductDetailDto?>.Failure("Product not found.", 404);

        return ServiceResult<PublicProductDetailDto?>.Success(result);
    }
}