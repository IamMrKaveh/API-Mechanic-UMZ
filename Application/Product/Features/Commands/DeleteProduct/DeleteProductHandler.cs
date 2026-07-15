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
        var product = await productRepository.GetByIdAsync(productId, ct);
        if (product is null)
            return ServiceResult.NotFound("محصول یافت نشد.");

        product.Deactivate();
        productRepository.Update(product);

        return ServiceResult.Success();
    }
}