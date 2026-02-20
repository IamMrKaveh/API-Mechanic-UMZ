namespace Application.Product.Features.Commands.CreateProduct;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, int>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = Domain.Product.Product.Create(
            Domain.Product.ValueObjects.ProductName.Create(request.Input.Name),
            Slug.Create(request.Input.Slug),
            request.Input.CategoryId,
            request.Input.BrandId,
            request.Input.Description
        );

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}