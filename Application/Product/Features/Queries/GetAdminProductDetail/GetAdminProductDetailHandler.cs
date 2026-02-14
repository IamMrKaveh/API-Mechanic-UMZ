namespace Application.Product.Features.Queries.GetAdminProductDetail;

public class GetAdminProductDetailHandler
    : IRequestHandler<GetAdminProductDetailQuery, ServiceResult<AdminProductDetailDto?>>
{
    private readonly IProductQueryService _productQueryService;

    public GetAdminProductDetailHandler(IProductQueryService productQueryService)
    {
        _productQueryService = productQueryService;
    }

    public async Task<ServiceResult<AdminProductDetailDto?>> Handle(
        GetAdminProductDetailQuery request, CancellationToken ct)
    {
        var result = await _productQueryService.GetAdminProductDetailAsync(request.ProductId, ct);
        if (result == null)
            return ServiceResult<AdminProductDetailDto?>.Failure("Product not found.", 404);

        return ServiceResult<AdminProductDetailDto?>.Success(result);
    }
}