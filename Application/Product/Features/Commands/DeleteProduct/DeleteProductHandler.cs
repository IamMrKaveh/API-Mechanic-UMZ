using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;

namespace Application.Product.Features.Commands.DeleteProduct;

public sealed class DeleteProductHandler(
    IProductRepository productRepository)
    : ICommandHandler<DeleteProductCommand>
{
    public async Task<ServiceResult> Handle(
        DeleteProductCommand request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);
        var productResult = await (productRepository.GetByIdAsync(productId, ct)).OrNotFoundAsync("محصول یافت نشد.");
        if (productResult.IsFailure) return productResult.Error;
        var product = productResult.Value;

        product.Deactivate();
        productRepository.Update(product);

        return ServiceResult.Success();
    }
}