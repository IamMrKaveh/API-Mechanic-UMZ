using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Domain.Common.ValueObjects;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Product.Features.Commands.UpdateProductDetails;

public sealed class UpdateProductDetailsHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateProductDetailsCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(UpdateProductDetailsCommand request, CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);
        var userId = UserId.From(currentUserService.CurrentUser.UserId);

        var product = await productRepository.GetByIdAsync(productId, ct);
        if (product is null)
            return ServiceResult.NotFound("محصول یافت نشد.");

        productRepository.SetOriginalRowVersion(product, Convert.FromBase64String(request.RowVersion));

        var slug = Slug.GenerateFrom(request.Name);

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

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            await auditService.LogProductEventAsync(
                productId,
                "UpdateProductDetails",
                "جزئیات محصول ویرایش شد.",
                userId);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("این محصول توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را رفرش کنید.");
        }
    }
}