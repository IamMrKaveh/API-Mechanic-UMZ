using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Product.Features.Commands.DeleteProduct;

public sealed class DeleteProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<DeleteProductCommand, ServiceResult>
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
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogProductEventAsync(
            product.Id,
            "DeleteProduct",
            $"محصول '{product.Name}' (Id={product.Id.Value}) حذف نرم شد.",
            UserId.From(request.DeletedByUserId));

        return ServiceResult.Success();
    }
}