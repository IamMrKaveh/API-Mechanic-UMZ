namespace Application.Product.Features.Queries.GetProductById;

public class GetProductByIdHandler
    : IRequestHandler<GetProductByIdQuery, ServiceResult<PublicProductDetailDto?>>
{
    private readonly IProductQueryService _productQueryService;

    public GetProductByIdHandler(IProductQueryService productQueryService)
    {
        _productQueryService = productQueryService;
    }

    public async Task<ServiceResult<PublicProductDetailDto?>> Handle(
        GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _productQueryService.GetPublicProductDetailAsync(
            request.Id, cancellationToken);

        if (result == null)
            return ServiceResult<PublicProductDetailDto?>.Failure("Product not found.", 404);

        return ServiceResult<PublicProductDetailDto?>.Success(result);
    }
}