using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Product.Interfaces;

namespace Application.Product.Features.Commands.RestoreProduct;

public class RestoreProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICacheService cacheService) : IRequestHandler<RestoreProductCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICacheService _cacheService = cacheService;

    public async Task<ServiceResult> Handle(
        RestoreProductCommand request,
        CancellationToken ct)
    {
        var product = await _productRepository.GetByIdIncludingDeletedAsync(request.Id);
        if (product == null)
            return ServiceResult.NotFound("Product not found.");

        product.Restore();

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(request.Id, "RestoreProduct", $"Product '{product.Name}' restored.", request.UserId);

        await _cacheService.ClearAsync($"product:{request.Id}");
        await _cacheService.ClearAsync($"brand:{product.BrandId}");

        return ServiceResult.Success();
    }
}