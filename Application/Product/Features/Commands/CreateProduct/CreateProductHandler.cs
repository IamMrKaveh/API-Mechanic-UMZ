using Domain.Product.Interfaces;

namespace Application.Product.Features.Commands.CreateProduct;

public sealed class CreateProductHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateProductCommand, ServiceResult<int>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public async Task<ServiceResult<int>> Handle(
        CreateProductCommand request,
        CancellationToken ct)
    {
        var product = Domain.Product.Aggregates.Product.Create(
            request.Name,
            request.Description,
            request.Price,
            request.CategoryId,
            request.BrandId,
            _dateTimeProvider.UtcNow);

        await _productRepository.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<int>.Success(product.Id);
    }
}