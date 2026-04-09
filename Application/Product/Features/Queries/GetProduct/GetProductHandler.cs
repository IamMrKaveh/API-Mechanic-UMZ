using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetProduct;

public class GetProductHandler(
    IProductQueryService productQueryService) : IRequestHandler<GetProductQuery, ServiceResult<ProductDetailDto>>
{
    private readonly IProductQueryService _productQueryService = productQueryService;

    public async Task<ServiceResult<ProductDetailDto>> Handle(
        GetProductQuery request,
        CancellationToken ct)
    {
        var product = await _productQueryService.GetProductDetailAsync(request.Id, ct);
        return product is null
            ? ServiceResult<ProductDetailDto>.NotFound("محصول یافت نشد.")
            : ServiceResult<ProductDetailDto>.Success(product);
    }
}