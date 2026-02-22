namespace Application.Product.Features.Commands.UpdateProduct;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProductHandler(
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

    public async Task<ServiceResult> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.UpdateProductInput.Id, cancellationToken);
        if (product == null)
            return ServiceResult.Failure("Product not found.");

        if (!string.IsNullOrEmpty(request.UpdateProductInput.Sku) && await _productRepository.ExistsBySkuAsync(request.UpdateProductInput.Sku, request.UpdateProductInput.Id, cancellationToken))
            return ServiceResult.Failure("Product SKU already exists.");

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
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.LogProductEventAsync(request.UpdateProductInput.Id, "UpdateProduct", "Product details updated", _currentUserService.UserId);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure("Concurrency Conflict: The record was modified by another user.");
        }
    }
}