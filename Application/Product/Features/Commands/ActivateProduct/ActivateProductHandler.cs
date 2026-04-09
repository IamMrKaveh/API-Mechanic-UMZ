using Domain.Common.Exceptions;
using Domain.Product.Interfaces;
using SharedKernel.Contracts;

namespace Application.Product.Features.Commands.ActivateProduct;

public class ActivateProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<ActivateProductCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult> Handle(ActivateProductCommand request, CancellationToken ct)
    {
        var product = await _productRepository.GetByIdWithVariantsAsync(request.ProductId, ct);
        if (product == null)
            return ServiceResult.NotFound("Product not found.");

        try
        {
            product.Activate();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(
            product.Id,
            "ActivateProduct",
            $"Product '{product.Name}' activated.",
            _currentUserService.CurrentUser.UserId);

        return ServiceResult.Success();
    }
}