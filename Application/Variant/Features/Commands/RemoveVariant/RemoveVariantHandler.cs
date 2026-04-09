using Domain.Common.Exceptions;
using Domain.Product.Interfaces;
using SharedKernel.Contracts;

namespace Application.Variant.Features.Commands.RemoveVariant;

public class RemoveVariantHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<RemoveVariantCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult> Handle(
        RemoveVariantCommand request,
        CancellationToken ct)
    {
        var product = await _productRepository.GetByIdWithVariantsAsync(request.ProductId, ct);
        if (product == null)
            return ServiceResult.NotFound("Product not found.");

        try
        {
            product.RemoveVariant(request.VariantId, _currentUserService.UserId);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
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