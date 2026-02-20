using Application.Audit.Contracts;
using Application.Security.Contracts;

namespace Application.Variant.Features.Commands.RemoveVariant;

public class RemoveVariantHandler : IRequestHandler<RemoveVariantCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public RemoveVariantHandler(
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

    public async Task<ServiceResult> Handle(RemoveVariantCommand request, CancellationToken ct)
    {
        var product = await _productRepository.GetByIdWithVariantsAsync(request.ProductId, ct);
        if (product == null)
            return ServiceResult.Failure("Product not found.");

        try
        {
            product.RemoveVariant(request.VariantId, _currentUserService.UserId);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(
            product.Id, "RemoveVariant",
            $"Variant {request.VariantId} soft-deleted from product '{product.Name}'.",
            _currentUserService.UserId);

        return ServiceResult.Success();
    }
}