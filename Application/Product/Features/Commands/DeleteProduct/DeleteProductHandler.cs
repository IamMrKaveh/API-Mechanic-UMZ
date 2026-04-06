using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;

namespace Application.Product.Features.Commands.DeleteProduct;

public class DeleteProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteProductHandler> logger) : IRequestHandler<DeleteProductCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<DeleteProductHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        DeleteProductCommand request,
        CancellationToken ct)
    {
        var product = await _productRepository.GetByIdAsync(ProductId.From(request.Id), ct);
        if (product is null)
            return ServiceResult.NotFound("محصول یافت نشد.");

        product.Deactivate();
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} deleted by user {UserId}", request.Id, request.DeletedByUserId);
        return ServiceResult.Success();
    }
}