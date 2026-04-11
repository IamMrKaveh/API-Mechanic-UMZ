using Application.Common.Interfaces;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Product.Features.Commands.DeleteProduct;

public sealed class DeleteProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    ILogger<DeleteProductHandler> logger) : IRequestHandler<DeleteProductCommand, ServiceResult>
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

        logger.LogInformation(
            "Product {ProductId} deleted by user {UserId}",
            request.ProductId,
            request.DeletedByUserId);

        await auditService.LogProductEventAsync(
            product.Id,
            "DeleteProduct",
            $"Product '{product.Name}' (Id={product.Id.Value}) soft-deleted.",
            UserId.From(request.DeletedByUserId));

        return ServiceResult.Success();
    }
}