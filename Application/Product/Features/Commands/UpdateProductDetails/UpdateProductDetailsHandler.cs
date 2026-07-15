using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;

namespace Application.Product.Features.Commands.UpdateProductDetails;

public sealed class UpdateProductDetailsHandler(
    IProductRepository productRepository)
    : ICommandHandler<UpdateProductDetailsCommand>
{
    public async Task<ServiceResult> Handle(UpdateProductDetailsCommand request, CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);

        var product = await productRepository.GetByIdAsync(productId, ct);
        if (product is null)
            return ServiceResult.NotFound("محصول یافت نشد.");

        productRepository.SetOriginalRowVersion(product, Convert.FromBase64String(request.RowVersion));

        var slug = ProductSlug.GenerateFrom(request.Name);

        if (await productRepository.ExistsBySlugAsync(slug, productId, ct))
            return ServiceResult.Conflict("محصولی با این نام قبلاً ثبت شده است.");

        product.UpdateDetails(
            ProductName.Create(request.Name),
            slug,
            request.Description ?? string.Empty);

        if (request.IsActive && !product.IsActive)
            product.Activate();
        else if (!request.IsActive && product.IsActive)
            product.Deactivate();

        productRepository.Update(product);

        return ServiceResult.Success();
    }
}