using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;

namespace Application.Product.Features.Commands.RestoreProduct;

public class RestoreProductHandler(
    IProductRepository productRepository)
    : ICommandHandler<RestoreProductCommand>
{
    public async Task<ServiceResult> Handle(
        RestoreProductCommand request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);

        var productResult = await (productRepository.GetByIdAsync(productId, ct)).OrNotFoundAsync("Product not found.");
        if (productResult.IsFailure) return productResult.Error;
        var product = productResult.Value;

        product.Restore();

        productRepository.Update(product);


        return ServiceResult.Success();
    }
}