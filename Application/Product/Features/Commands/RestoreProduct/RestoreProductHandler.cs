namespace Application.Product.Features.Commands.RestoreProduct;

public class RestoreProductHandler : IRequestHandler<RestoreProductCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICacheService _cacheService;

    public RestoreProductHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _cacheService = cacheService;
    }

    public async Task<ServiceResult> Handle(RestoreProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdIncludingDeletedAsync(request.Id);
        if (product == null)
        {
            return ServiceResult.Failure("Product not found.");
        }

        product.Restore();

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogProductEventAsync(request.Id, "RestoreProduct", $"Product '{product.Name}' restored.", request.UserId);

        await _cacheService.ClearAsync($"product:{request.Id}");
        await _cacheService.ClearAsync($"categorygroup:{product.BrandId}");

        return ServiceResult.Success();
    }
}