using Domain.Product.Interfaces;
using SharedKernel.Contracts;

namespace Application.Product.Features.Commands.DeactivateProduct;

public class DeactivateProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<DeactivateProductCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult> Handle(
        DeactivateProductCommand request,
        CancellationToken ct)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, ct);
        if (product == null)
            return ServiceResult.NotFound("Product not found.");

        product.Deactivate();
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(
            product.Id.Value,
            "DeactivateProduct",
            $"Product '{product.Name}' deactivated.",
            _currentUserService.CurrentUser.UserId);

        return ServiceResult.Success();
    }
}