namespace Application.Product.Features.Queries.GetAdminProductById;

public class GetAdminProductByIdHandler
    : IRequestHandler<GetAdminProductByIdQuery, ServiceResult<AdminProductDetailDto?>>
{
    private readonly IProductQueryService _productQueryService;

    public GetAdminProductByIdHandler(IProductQueryService productQueryService)
    {
        _productQueryService = productQueryService;
    }

    public async Task<ServiceResult<AdminProductDetailDto?>> Handle(
        GetAdminProductByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _productQueryService.GetAdminProductDetailAsync(
            request.ProductId, cancellationToken);

        if (result == null)
            return ServiceResult<AdminProductDetailDto?>.Failure("Product not found.", 404);

        return ServiceResult<AdminProductDetailDto?>.Success(result);
    }
}