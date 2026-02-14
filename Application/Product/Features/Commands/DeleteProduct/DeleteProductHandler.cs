using Application.Audit.Contracts;
using Application.Security.Contracts;

namespace Application.Product.Features.Commands.DeleteProduct;

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public DeleteProductHandler(
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

    public async Task<ServiceResult> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await _productRepository.GetByIdWithVariantsAsync(request.Id, ct);
        if (product == null)
            return ServiceResult.Failure("Product not found.");

        product.MarkAsDeleted(_currentUserService.UserId);
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(
            request.Id, "DeleteProduct", $"Product '{product.Name}' soft-deleted.", _currentUserService.UserId);

        return ServiceResult.Success();
    }
}