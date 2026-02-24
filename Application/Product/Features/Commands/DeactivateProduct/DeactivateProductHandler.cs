namespace Application.Product.Features.Commands.DeactivateProduct;

public class DeactivateProductHandler : IRequestHandler<DeactivateProductCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public DeactivateProductHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResult> Handle(DeactivateProductCommand request, CancellationToken ct)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, ct);
        if (product == null)
            return ServiceResult.Failure("Product not found.");

        product.Deactivate();
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(
            product.Id, "DeactivateProduct", $"Product '{product.Name}' deactivated.", _currentUserService.UserId);

        return ServiceResult.Success();
    }
}