using Application.Audit.Contracts;
using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Product.Interfaces;
using SharedKernel.Contracts;

namespace Application.Product.Features.Commands.DeleteProduct;

public class DeleteProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<DeleteProductCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await _productRepository.GetByIdWithVariantsAsync(request.Id, ct);
        if (product == null)
            return ServiceResult.NotFound("Product not found.");

        product.Delete(_currentUserService.CurrentUser.UserId);
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(
            request.Id,
            "DeleteProduct",
            $"Product '{product.Name}' soft-deleted.",
            _currentUserService.CurrentUser.UserId);

        return ServiceResult.Success();
    }
}