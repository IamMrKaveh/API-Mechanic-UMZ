using Application.Audit.Contracts;
using Application.Security.Contracts;

namespace Application.Product.Features.Commands.ChangePrice;

public class ChangePriceHandler : IRequestHandler<ChangePriceCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;

    public ChangePriceHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<ServiceResult> Handle(ChangePriceCommand request, CancellationToken ct)
    {
        var product = await _productRepository.GetByIdWithVariantsAsync(request.ProductId, ct);
        if (product == null)
            return ServiceResult.Failure("Product not found.");

        try
        {
            product.ChangeVariantPrices(
                request.VariantId,
                request.PurchasePrice,
                request.SellingPrice,
                request.OriginalPrice);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(
            product.Id, "ChangePrice",
            $"Variant {request.VariantId} prices changed. Selling: {request.SellingPrice}, Original: {request.OriginalPrice}",
            _currentUserService.UserId);

        await _cacheService.ClearAsync($"product:{product.Id}");

        return ServiceResult.Success();
    }
}