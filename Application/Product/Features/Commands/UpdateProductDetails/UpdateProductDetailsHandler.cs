namespace Application.Product.Features.Commands.UpdateProductDetails;

public class UpdateProductDetailsHandler : IRequestHandler<UpdateProductDetailsCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProductDetailsHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IHtmlSanitizer htmlSanitizer,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _htmlSanitizer = htmlSanitizer;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResult> Handle(UpdateProductDetailsCommand request, CancellationToken ct)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, ct);
        if (product == null) return ServiceResult.Failure("Product not found.");

        _productRepository.SetOriginalRowVersion(product, Convert.FromBase64String(request.RowVersion));

        if (!string.IsNullOrEmpty(request.Sku)
            && await _productRepository.ExistsBySkuAsync(request.Sku, request.Id, ct))
            return ServiceResult.Failure("Product SKU already exists.");

        product.UpdateDetails(
            _htmlSanitizer.Sanitize(request.Name),
            request.Description != null ? _htmlSanitizer.Sanitize(request.Description) : null,
            request.Sku,
            request.CategoryGroupId,
            request.IsActive);

        _productRepository.Update(product);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
            await _auditService.LogProductEventAsync(
                request.Id, "UpdateProductDetails", "Product details updated.", _currentUserService.UserId);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure("This product was modified by another user. Please refresh and try again.");
        }
    }
}