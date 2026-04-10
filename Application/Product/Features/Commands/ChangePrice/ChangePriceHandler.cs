using Application.Common.Interfaces;
using Domain.Common.Exceptions;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Product.Features.Commands.ChangePrice;

public class ChangePriceHandler(
    IVariantRepository variantRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    ICacheService cacheService) : IRequestHandler<ChangePriceCommand, ServiceResult>
{
    private readonly IVariantRepository _variantRepository = variantRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ICacheService _cacheService = cacheService;

    public async Task<ServiceResult> Handle(
        ChangePriceCommand request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var productId = ProductId.From(request.ProductId);
        var userId = UserId.From(request.ProductId);

        var variant = await _variantRepository.GetByIdAsync(variantId, ct);
        if (variant is null || variant.ProductId != productId)
            return ServiceResult.NotFound("Variant not found.");

        try
        {
            variant.SetPricing(request.PurchasePrice, request.SellingPrice, request.OriginalPrice);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        _variantRepository.Update(variant);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(
            productId,
            "ChangePrice",
            $"Variant {request.VariantId} prices changed. Selling: {request.SellingPrice}, Original: {request.OriginalPrice}",
            userId);

        await _cacheService.RemoveAsync($"product:{request.ProductId}", ct);
        await _cacheService.RemoveAsync($"variant:{request.VariantId}", ct);

        return ServiceResult.Success();
    }
}