using Application.Audit.Contracts;
using Application.Common.Exceptions;
using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Product.Interfaces;
using SharedKernel.Contracts;

namespace Application.Product.Features.Commands.UpdateProduct;

public class UpdateProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IHtmlSanitizer htmlSanitizer,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateProductCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IHtmlSanitizer _htmlSanitizer = htmlSanitizer;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult> Handle(
        UpdateProductCommand request,
        CancellationToken ct)
    {
        var product = await _productRepository.GetByIdAsync(request.UpdateProductInput.Id, ct);
        if (product == null)
            return ServiceResult.NotFound("Product not found.");

        if (!string.IsNullOrEmpty(request.UpdateProductInput.Sku) && await _productRepository.ExistsBySkuAsync(request.UpdateProductInput.Sku, request.UpdateProductInput.Id, ct))
            return ServiceResult.Conflict("Product SKU already exists.");

        if (!string.IsNullOrEmpty(request.UpdateProductInput.RowVersion))
            _productRepository.SetOriginalRowVersion(product, System.Convert.FromBase64String(request.UpdateProductInput.RowVersion));

        product.UpdateDetails(
            _htmlSanitizer.Sanitize(request.UpdateProductInput.Name),
            _htmlSanitizer.Sanitize(request.UpdateProductInput.Description ?? string.Empty),
            request.UpdateProductInput.Sku,
            request.UpdateProductInput.BrandId,
            request.UpdateProductInput.IsActive);

        _productRepository.Update(product);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
            await _auditService.LogProductEventAsync(request.UpdateProductInput.Id, "UpdateProduct", "Product details updated", _currentUserService.UserId);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("Concurrency Conflict: The record was modified by another user.");
        }
    }
}